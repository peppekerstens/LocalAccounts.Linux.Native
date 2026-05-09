// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics;
using System.Globalization;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// Internal helpers for invoking Linux account management tools
    /// (getent, passwd, chage, useradd, usermod, userdel, groupadd, groupmod, groupdel, gpasswd, chpasswd).
    /// </summary>
    internal static class AccountHelpers
    {
        // ------------------------------------------------------------------ //
        //  Process execution                                                  //
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
            string stdout = proc.StandardOutput.ReadToEnd();
            string stderr = proc.StandardError.ReadToEnd();
            proc.WaitForExit();
            return (proc.ExitCode, stdout, stderr);
        }

        /// <summary>Run a command with stdin piped from <paramref name="stdin"/>.</summary>
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
            string stdout = proc.StandardOutput.ReadToEnd();
            string stderr = proc.StandardError.ReadToEnd();
            proc.WaitForExit();
            return (proc.ExitCode, stdout, stderr);
        }

        // ------------------------------------------------------------------ //
        //  getent helpers                                                     //
        // ------------------------------------------------------------------ //

        /// <summary>
        /// Returns all passwd entries as parsed <see cref="LocalUser"/> objects.
        /// </summary>
        internal static IEnumerable<LocalUser> GetAllUsers()
        {
            var (_, stdout, _) = Run("getent", "passwd");
            foreach (var line in stdout.Split('\n', StringSplitOptions.RemoveEmptyEntries))
            {
                var user = ParsePasswdLine(line);
                if (user is not null)
                    yield return user;
            }
        }

        internal static LocalUser? GetUser(string name)
        {
            var (exit, stdout, _) = Run("getent", "passwd", name);
            if (exit != 0 || string.IsNullOrWhiteSpace(stdout)) return null;
            return ParsePasswdLine(stdout.Trim());
        }

        private static LocalUser? ParsePasswdLine(string line)
        {
            var f = line.Split(':');
            if (f.Length < 7) return null;

            if (!int.TryParse(f[2], out int uid)) return null;
            if (!int.TryParse(f[3], out int gid)) return null;

            string gecos    = f[4];
            string fullName = gecos.Split(',')[0];

            // password status
            bool enabled          = true;
            bool passwordRequired = true;
            var (_, pstat, _) = Run("passwd", "-S", f[0]);
            if (!string.IsNullOrEmpty(pstat))
            {
                var parts = pstat.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 2)
                {
                    if (parts[1] is "L" or "LK") enabled          = false;
                    if (parts[1] == "NP")         passwordRequired = false;
                }
            }

            // chage
            DateTime? passwordExpires        = null;
            DateTime? passwordLastSet        = null;
            DateTime? accountExpires         = null;
            DateTime? passwordChangeableDate = null;

            var (_, chage, _) = Run("chage", "-l", f[0]);
            foreach (var cl in chage.Split('\n'))
            {
                TryParseChageDate(cl, "Password expires", ref passwordExpires);
                TryParseChageDate(cl, "Last password change", ref passwordLastSet);
                TryParseChageDate(cl, "Account expires", ref accountExpires);
            }

            return new LocalUser
            {
                Name                   = f[0],
                FullName               = fullName,
                Description            = gecos,
                Enabled                = enabled,
                SID                    = null,
                PasswordRequired       = passwordRequired,
                UserMayChangePassword  = true,
                PasswordExpires        = passwordExpires,
                PasswordLastSet        = passwordLastSet,
                PasswordChangeableDate = passwordChangeableDate,
                AccountExpires         = accountExpires,
                LastLogon              = null,
                HomeDirectory          = f[5],
                Shell                  = f[6],
                UID                    = uid,
                GID                    = gid,
            };
        }

        private static void TryParseChageDate(string line, string label, ref DateTime? target)
        {
            var idx = line.IndexOf(label, StringComparison.OrdinalIgnoreCase);
            if (idx < 0) return;
            var colon = line.IndexOf(':', idx + label.Length);
            if (colon < 0) return;
            var val = line[(colon + 1)..].Trim();
            if (val is "never" or "password must be changed") return;
            if (DateTime.TryParse(val, CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out var dt))
                target = dt;
        }

        // ------------------------------------------------------------------ //
        //  getent group helpers                                               //
        // ------------------------------------------------------------------ //

        internal static IEnumerable<LocalGroup> GetAllGroups()
        {
            var (_, stdout, _) = Run("getent", "group");
            foreach (var line in stdout.Split('\n', StringSplitOptions.RemoveEmptyEntries))
            {
                var g = ParseGroupLine(line);
                if (g is not null) yield return g;
            }
        }

        internal static LocalGroup? GetGroup(string name)
        {
            var (exit, stdout, _) = Run("getent", "group", name);
            if (exit != 0 || string.IsNullOrWhiteSpace(stdout)) return null;
            return ParseGroupLine(stdout.Trim());
        }

        private static LocalGroup? ParseGroupLine(string line)
        {
            var f = line.Split(':');
            if (f.Length < 4) return null;
            if (!int.TryParse(f[2], out int gid)) return null;
            return new LocalGroup
            {
                Name            = f[0],
                Description     = string.Empty,
                SID             = null,
                ObjectClass     = "Group",
                PrincipalSource = "Local",
                GID             = gid,
            };
        }

        internal static IEnumerable<LocalPrincipal> GetGroupMembers(string groupName)
        {
            var (exit, stdout, _) = Run("getent", "group", groupName);
            if (exit != 0 || string.IsNullOrWhiteSpace(stdout))
                yield break;

            var f = stdout.Trim().Split(':');
            if (f.Length < 4 || !int.TryParse(f[2], out int gid))
                yield break;

            var members = new HashSet<string>(StringComparer.Ordinal);

            // explicit members in field[3]
            if (!string.IsNullOrEmpty(f[3]))
                foreach (var m in f[3].Split(',', StringSplitOptions.RemoveEmptyEntries))
                    members.Add(m);

            // primary group members
            var (_, pstdout, _) = Run("getent", "passwd");
            foreach (var line in pstdout.Split('\n', StringSplitOptions.RemoveEmptyEntries))
            {
                var pf = line.Split(':');
                if (pf.Length >= 4 && int.TryParse(pf[3], out int pgid) && pgid == gid)
                    members.Add(pf[0]);
            }

            foreach (var m in members.OrderBy(x => x))
                yield return new LocalPrincipal { Name = m, ObjectClass = "User", PrincipalSource = "Local" };
        }

        // ------------------------------------------------------------------ //
        //  Password helper                                                    //
        // ------------------------------------------------------------------ //

        internal static void SetPassword(string username, System.Security.SecureString password)
        {
            var plain = new System.Net.NetworkCredential(string.Empty, password).Password;
            RunWithStdin($"{username}:{plain}", "chpasswd");
        }
    }
}
