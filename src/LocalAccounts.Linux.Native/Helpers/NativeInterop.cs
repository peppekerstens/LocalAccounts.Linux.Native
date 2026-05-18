// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Runtime.InteropServices;

namespace Microsoft.PowerShell.Commands;
    /// <summary>
    /// P/Invoke bindings for libc user/group/shadow database functions.
    ///
    /// All functions are available on every major Linux libc (glibc, musl).
    /// The library name "libc" resolves correctly on Linux via the runtime's
    /// built-in search (maps to libc.so.6 / libc.musl-*.so.1).
    ///
    /// Thread-safety: the *ent() family functions use global state; wrap
    /// calls in a lock when called from multiple threads.
    /// </summary>
    internal static partial class NativeInterop
    {
        private const string Libc = "libc";

        // ------------------------------------------------------------------ //
        //  passwd database — /etc/passwd (+ nsswitch sources via getpwent)   //
        // ------------------------------------------------------------------ //

        /// <summary>
        /// Rewind the passwd database to the beginning.
        /// Must be called before the first <see cref="GetPwEnt"/> call.
        /// </summary>
        [LibraryImport(Libc, EntryPoint = "setpwent")]
        internal static partial void SetPwEnt();

        /// <summary>Returns the next entry from the passwd database, or <see cref="IntPtr.Zero"/> when exhausted.</summary>
        [LibraryImport(Libc, EntryPoint = "getpwent")]
        internal static partial IntPtr GetPwEnt();

        /// <summary>Look up a passwd entry by name. Returns <see cref="IntPtr.Zero"/> if not found.</summary>
        [LibraryImport(Libc, EntryPoint = "getpwnam", StringMarshalling = StringMarshalling.Utf8)]
        internal static partial IntPtr GetPwNam(string name);

        /// <summary>Close the passwd database after enumeration.</summary>
        [LibraryImport(Libc, EntryPoint = "endpwent")]
        internal static partial void EndPwEnt();

        // ------------------------------------------------------------------ //
        //  shadow database — /etc/shadow (requires root on most distros)     //
        // ------------------------------------------------------------------ //

        /// <summary>
        /// Look up a shadow entry by user name.
        /// Returns <see cref="IntPtr.Zero"/> if not found or if caller lacks
        /// permission to read the shadow database (non-root on most systems).
        /// </summary>
        [LibraryImport(Libc, EntryPoint = "getspnam", StringMarshalling = StringMarshalling.Utf8)]
        internal static partial IntPtr GetSpNam(string name);

        // ------------------------------------------------------------------ //
        //  group database — /etc/group (+ nsswitch sources via getgrent)     //
        // ------------------------------------------------------------------ //

        /// <summary>Rewind the group database.</summary>
        [LibraryImport(Libc, EntryPoint = "setgrent")]
        internal static partial void SetGrEnt();

        /// <summary>Returns the next entry from the group database, or <see cref="IntPtr.Zero"/> when exhausted.</summary>
        [LibraryImport(Libc, EntryPoint = "getgrent")]
        internal static partial IntPtr GetGrEnt();

        /// <summary>Look up a group entry by name. Returns <see cref="IntPtr.Zero"/> if not found.</summary>
        [LibraryImport(Libc, EntryPoint = "getgrnam", StringMarshalling = StringMarshalling.Utf8)]
        internal static partial IntPtr GetGrNam(string name);

        /// <summary>Close the group database after enumeration.</summary>
        [LibraryImport(Libc, EntryPoint = "endgrent")]
        internal static partial void EndGrEnt();

        // ------------------------------------------------------------------ //
        //  Struct definitions                                                  //
        //                                                                      //
        //  The C structs are returned as raw IntPtr (pointer to static         //
        //  storage inside libc).  We marshal them by overlaying a managed      //
        //  struct on the native memory via Marshal.PtrToStructure.             //
        //                                                                      //
        //  IMPORTANT: these structs must NOT be copied by value after          //
        //  getpwent/getgrent is called again — the pointer is overwritten.     //
        //  Always copy the string fields immediately.                           //
        // ------------------------------------------------------------------ //

        /// <summary>
        /// Mirrors the C <c>struct passwd</c> from <c>pwd.h</c>.
        /// Field order and types are identical on x86-64 and arm64 glibc/musl.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        internal struct Passwd
        {
            public IntPtr pw_name;    // char* — login name
            public IntPtr pw_passwd;  // char* — encrypted password (usually "x")
            public uint   pw_uid;     // uid_t
            public uint   pw_gid;     // gid_t
            public IntPtr pw_gecos;   // char* — real name / GECOS
            public IntPtr pw_dir;     // char* — home directory
            public IntPtr pw_shell;   // char* — shell
        }

        /// <summary>
        /// Mirrors the C <c>struct spwd</c> from <c>shadow.h</c>.
        /// sp_lstchg / sp_min / sp_max / sp_expire are days since 1970-01-01.
        /// A value of -1 means "not set".
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        internal struct Spwd
        {
            public IntPtr sp_namp;    // char* — login name
            public IntPtr sp_pwdp;    // char* — hashed password; "!" prefix = locked; "!!" or "" = no password
            public long   sp_lstchg;  // date of last change
            public long   sp_min;     // min days before change allowed
            public long   sp_max;     // max days before change required (99999 = never expires)
            public long   sp_warn;    // days before expiry to warn user
            public long   sp_inact;   // days after expiry before account locked
            public long   sp_expire;  // date when account expires (-1 = never)
            public ulong  sp_flag;    // reserved
        }

        /// <summary>
        /// Mirrors the C <c>struct group</c> from <c>grp.h</c>.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        internal struct Group
        {
            public IntPtr gr_name;    // char* — group name
            public IntPtr gr_passwd;  // char* — group password (usually "x" or "")
            public uint   gr_gid;     // gid_t
            public IntPtr gr_mem;     // char** — null-terminated array of member name pointers
        }

        // ------------------------------------------------------------------ //
        //  Marshalling helpers                                                 //
        // ------------------------------------------------------------------ //

        internal static string PtrToString(IntPtr ptr) =>
            ptr == IntPtr.Zero ? string.Empty : Marshal.PtrToStringAnsi(ptr) ?? string.Empty;

        /// <summary>
        /// Reads a null-terminated array of C strings (char**) into a managed string array.
        /// </summary>
        internal static string[] PtrToStringArray(IntPtr arrayPtr)
        {
            if (arrayPtr == IntPtr.Zero) return Array.Empty<string>();

            var result = new List<string>();
            int offset = 0;
            while (true)
            {
                var strPtr = Marshal.ReadIntPtr(arrayPtr, offset * IntPtr.Size);
                if (strPtr == IntPtr.Zero) break;
                result.Add(Marshal.PtrToStringAnsi(strPtr) ?? string.Empty);
                offset++;
            }
            return result.ToArray();
        }

        /// <summary>
        /// Marshal an IntPtr returned by getpwent/getpwnam into a managed <see cref="Passwd"/> copy.
        /// Returns null if ptr is Zero.
        /// </summary>
        internal static Passwd? MarshalPasswd(IntPtr ptr)
        {
            if (ptr == IntPtr.Zero) return null;
            return Marshal.PtrToStructure<Passwd>(ptr);
        }

        /// <summary>
        /// Marshal an IntPtr returned by getspnam into a managed <see cref="Spwd"/> copy.
        /// Returns null if ptr is Zero.
        /// </summary>
        internal static Spwd? MarshalSpwd(IntPtr ptr)
        {
            if (ptr == IntPtr.Zero) return null;
            return Marshal.PtrToStructure<Spwd>(ptr);
        }

        /// <summary>
        /// Marshal an IntPtr returned by getgrent/getgrnam into a managed <see cref="Group"/> copy.
        /// Returns null if ptr is Zero.
        /// </summary>
        internal static Group? MarshalGroup(IntPtr ptr)
        {
            if (ptr == IntPtr.Zero) return null;
            return Marshal.PtrToStructure<Group>(ptr);
        }

        // ------------------------------------------------------------------ //
        //  Date conversion helpers                                             //
        // ------------------------------------------------------------------ //

        private static readonly DateTime s_epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        /// <summary>
        /// Convert a shadow day-count (-1 = never) to a nullable DateTime.
        /// </summary>
        internal static DateTime? ShadowDaysToDate(long days) =>
            days <= 0 ? null : s_epoch.AddDays(days).ToLocalTime();
    }
