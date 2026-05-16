// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// Helpers for LocalAccounts cmdlets on Linux.
    ///
    /// READ operations use P/Invoke libc calls (getpwent/getpwnam/getgrent/
    /// getgrnam/getspnam) — no subprocess spawning required.
    ///
    /// WRITE operations (create/modify/delete users and groups) use
    /// Process.Start to invoke the standard shadow-utils tools (useradd,
    /// usermod, userdel, groupadd, groupmod, groupdel, gpasswd, chpasswd).
    /// No POSIX syscall equivalent exists for these mutations.
    /// </summary>
    internal static class AccountHelpers
    {
        // ------------------------------------------------------------------ //
        //  Lock for non-reentrant getent family functions                     //
        // ------------------------------------------------------------------ //

        private static readonly object s_pwLock = new();
        private static readonly object s_grLock = new();

        // ------------------------------------------------------------------ //
        //  Process execution (write operations only)                          //
        // ------------------------------------------------------------------ //

        internal static (int ExitCode, string Stdout, string Stderr) Run(
            string executable, params string[] args)
        {
            var psi = new ProcessStartInfo(executable)
            {
                RedirectStandardOutput = true,
                RedirectStandardError  = true,
                UseShellExecute        = false,
            };
            foreach (var a in args)
                psi.ArgumentList.Add(a);

            using var proc = Process.Start(psi)!;
            // Read stdout and stderr concurrently to avoid deadlock when either
            // pipe buffer fills. Safe: cmdlets run on thread-pool threads with
            // no SynchronizationContext.
            var stdoutTask = proc.StandardOutput.ReadToEndAsync();
            var stderrTask = proc.StandardError.ReadToEndAsync();
            System.Threading.Tasks.Task.WaitAll(stdoutTask, stderrTask);
            proc.WaitForExit();
            return (proc.ExitCode, stdoutTask.Result, stderrTask.Result);
        }

        internal static (int ExitCode, string Stdout, string Stderr) RunWithStdin(
            string stdin, string executable, params string[] args)
        {
            var psi = new ProcessStartInfo(executable)
            {
                RedirectStandardInput  = true,
                RedirectStandardOutput = true,
                RedirectStandardError  = true,
                UseShellExecute        = false,
            };
            foreach (var a in args)
                psi.ArgumentList.Add(a);

            using var proc = Process.Start(psi)!;
            proc.StandardInput.WriteLine(stdin);
            proc.StandardInput.Close();
            // Read stdout and stderr concurrently to avoid deadlock when either
            // pipe buffer fills. Safe: cmdlets run on thread-pool threads with
            // no SynchronizationContext.
            var stdoutTask = proc.StandardOutput.ReadToEndAsync();
            var stderrTask = proc.StandardError.ReadToEndAsync();
            System.Threading.Tasks.Task.WaitAll(stdoutTask, stderrTask);
            proc.WaitForExit();
            return (proc.ExitCode, stdoutTask.Result, stderrTask.Result);
        }

        /// <summary>
        /// Checks if a subprocess failure is due to insufficient privileges.
        /// Returns true when stderr contains "Permission denied" or exit code is 4 (useradd/groupadd permission denied).
        /// </summary>
        internal static bool IsPermissionDenied(int exitCode, string stderr)
        {
            if (exitCode == 4) return true; // useradd/groupadd permission denied
            return stderr.Contains("Permission denied", StringComparison.OrdinalIgnoreCase)
                || stderr.Contains("Operation not permitted", StringComparison.OrdinalIgnoreCase);
        }

        // ------------------------------------------------------------------ //
        //  User read operations — P/Invoke libc                              //
        // ------------------------------------------------------------------ //

        /// <summary>
        /// Enumerate all users from the passwd database (honouring nsswitch.conf).
        /// Uses getpwent() — no subprocess.
        /// </summary>
        internal static IEnumerable<LocalUser> GetAllUsers()
        {
            var users = new List<LocalUser>();

            lock (s_pwLock)
            {
                NativeInterop.SetPwEnt();
                try
                {
                    IntPtr ptr;
                    while ((ptr = NativeInterop.GetPwEnt()) != IntPtr.Zero)
                    {
                        var pw = NativeInterop.MarshalPasswd(ptr);
                        if (pw is null) continue;
                        var user = BuildLocalUser(pw.Value);
                        if (user is not null)
                            users.Add(user);
                    }
                }
                finally
                {
                    NativeInterop.EndPwEnt();
                }
            }

            return users;
        }

        /// <summary>
        /// Look up a single user by name using getpwnam().
        /// Returns null if not found.
        /// </summary>
        internal static LocalUser? GetUser(string name)
        {
            lock (s_pwLock)
            {
                var ptr = NativeInterop.GetPwNam(name);
                if (ptr == IntPtr.Zero) return null;
                var pw = NativeInterop.MarshalPasswd(ptr);
                return pw is null ? null : BuildLocalUser(pw.Value);
            }
        }

        private static LocalUser? BuildLocalUser(NativeInterop.Passwd pw)
        {
            string username = NativeInterop.PtrToString(pw.pw_name);
            if (string.IsNullOrEmpty(username)) return null;

            string gecos    = NativeInterop.PtrToString(pw.pw_gecos);
            string fullName = gecos.Split(',')[0];

            // Default: assume enabled, password required
            bool enabled          = true;
            bool passwordRequired = true;
            DateTime? passwordExpires        = null;
            DateTime? passwordLastSet        = null;
            DateTime? passwordChangeableDate = null;
            DateTime? accountExpires         = null;

            // Read shadow entry — requires root; silently degrades if unavailable
            var spPtr = NativeInterop.GetSpNam(username);
            if (spPtr != IntPtr.Zero)
            {
                var sp = NativeInterop.MarshalSpwd(spPtr);
                if (sp.HasValue)
                {
                    string pwdp = NativeInterop.PtrToString(sp.Value.sp_pwdp);

                    // Locked: password hash starts with '!'
                    // No-password: empty hash or "!!"
                    if (pwdp.StartsWith('!') && pwdp.Length > 1 && pwdp[1] != '!')
                        enabled = false;
                    if (string.IsNullOrEmpty(pwdp) || pwdp == "!!" || pwdp == "!")
                        passwordRequired = false;

                    passwordLastSet        = NativeInterop.ShadowDaysToDate(sp.Value.sp_lstchg);
                    accountExpires         = NativeInterop.ShadowDaysToDate(sp.Value.sp_expire);

                    // Password expires: lastChange + max. If max is 99999 → never.
                    if (sp.Value.sp_max > 0 && sp.Value.sp_max < 99999 && sp.Value.sp_lstchg > 0)
                        passwordExpires = NativeInterop.ShadowDaysToDate(
                            sp.Value.sp_lstchg + sp.Value.sp_max);

                    // Earliest date user may change password: lastChange + min
                    if (sp.Value.sp_min > 0 && sp.Value.sp_lstchg > 0)
                        passwordChangeableDate = NativeInterop.ShadowDaysToDate(
                            sp.Value.sp_lstchg + sp.Value.sp_min);
                }
            }

            return new LocalUser
            {
                Name                   = username,
                FullName               = fullName,
                Description            = gecos,
                Enabled                = enabled,
                ObjectClass            = "User",
                PrincipalSource        = PrincipalSource.Local,
                PasswordRequired       = passwordRequired,
                UserMayChangePassword  = true,
                PasswordExpires        = passwordExpires,
                PasswordLastSet        = passwordLastSet,
                PasswordChangeableDate = passwordChangeableDate,
                AccountExpires         = accountExpires,
                LastLogon              = null,
                HomeDirectory          = NativeInterop.PtrToString(pw.pw_dir),
                Shell                  = NativeInterop.PtrToString(pw.pw_shell),
                UID                    = (int)pw.pw_uid,
                GID                    = (int)pw.pw_gid,
            };
        }

        // ------------------------------------------------------------------ //
        //  Group read operations — P/Invoke libc                             //
        // ------------------------------------------------------------------ //

        /// <summary>
        /// Enumerate all groups from the group database using getgrent().
        /// No subprocess.
        /// </summary>
        internal static IEnumerable<LocalGroup> GetAllGroups()
        {
            var groups = new List<LocalGroup>();

            lock (s_grLock)
            {
                NativeInterop.SetGrEnt();
                try
                {
                    IntPtr ptr;
                    while ((ptr = NativeInterop.GetGrEnt()) != IntPtr.Zero)
                    {
                        var g = NativeInterop.MarshalGroup(ptr);
                        if (g is null) continue;
                        var grp = BuildLocalGroup(g.Value);
                        if (grp is not null) groups.Add(grp);
                    }
                }
                finally
                {
                    NativeInterop.EndGrEnt();
                }
            }

            return groups;
        }

        /// <summary>
        /// Look up a single group by name using getgrnam().
        /// Returns null if not found.
        /// </summary>
        internal static LocalGroup? GetGroup(string name)
        {
            lock (s_grLock)
            {
                var ptr = NativeInterop.GetGrNam(name);
                if (ptr == IntPtr.Zero) return null;
                var g = NativeInterop.MarshalGroup(ptr);
                return g is null ? null : BuildLocalGroup(g.Value);
            }
        }

        private static LocalGroup? BuildLocalGroup(NativeInterop.Group g)
        {
            string name = NativeInterop.PtrToString(g.gr_name);
            if (string.IsNullOrEmpty(name)) return null;

            return new LocalGroup
            {
                Name            = name,
                Description     = string.Empty,
                ObjectClass     = "Group",
                PrincipalSource = PrincipalSource.Local,
                GID             = (int)g.gr_gid,
            };
        }

        // ------------------------------------------------------------------ //
        //  Group member resolution — P/Invoke libc                           //
        // ------------------------------------------------------------------ //

        /// <summary>
        /// Get all members of a group: explicit members (gr_mem) plus users
        /// whose primary GID matches this group, resolved via getpwent().
        /// No subprocess.
        /// </summary>
        internal static IEnumerable<LocalPrincipal> GetGroupMembers(string groupName)
        {
            LocalGroup? grp = GetGroup(groupName);
            if (grp is null) yield break;

            int targetGid = grp.GID;

            // Get explicit members from gr_mem
            var members = new HashSet<string>(StringComparer.Ordinal);

            lock (s_grLock)
            {
                var ptr = NativeInterop.GetGrNam(groupName);
                if (ptr != IntPtr.Zero)
                {
                    var g = NativeInterop.MarshalGroup(ptr);
                    if (g.HasValue)
                    {
                        foreach (var m in NativeInterop.PtrToStringArray(g.Value.gr_mem))
                            if (!string.IsNullOrEmpty(m)) members.Add(m);
                    }
                }
            }

            // Add users whose primary GID matches (primary group members)
            lock (s_pwLock)
            {
                NativeInterop.SetPwEnt();
                try
                {
                    IntPtr ptr;
                    while ((ptr = NativeInterop.GetPwEnt()) != IntPtr.Zero)
                    {
                        var pw = NativeInterop.MarshalPasswd(ptr);
                        if (pw is null) continue;
                        if ((int)pw.Value.pw_gid == targetGid)
                        {
                            string uname = NativeInterop.PtrToString(pw.Value.pw_name);
                            if (!string.IsNullOrEmpty(uname)) members.Add(uname);
                        }
                    }
                }
                finally
                {
                    NativeInterop.EndPwEnt();
                }
            }

            foreach (var m in members.OrderBy(x => x))
                yield return new LocalPrincipal
                {
                    Name            = m,
                    ObjectClass     = "User",
                    PrincipalSource = PrincipalSource.Local,
                };
        }

        // ------------------------------------------------------------------ //
        //  Password write helper                                              //
        // ------------------------------------------------------------------ //

        internal static void SetPassword(string username, System.Security.SecureString password)
        {
            var plain = new System.Net.NetworkCredential(string.Empty, password).Password;
            RunWithStdin($"{username}:{plain}", "chpasswd");
        }
    }
}
