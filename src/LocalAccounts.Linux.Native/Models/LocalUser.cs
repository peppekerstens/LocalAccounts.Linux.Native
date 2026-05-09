// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.PowerShell.Commands
{
    /// <summary>Represents a local user account on a Linux system.</summary>
    public class LocalUser
    {
        public string Name { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool Enabled { get; set; }
        public string? SID { get; set; }
        public string ObjectClass { get; set; } = "User";
        public string PrincipalSource { get; set; } = "Local";
        public bool PasswordRequired { get; set; }
        public bool UserMayChangePassword { get; set; } = true;
        public DateTime? PasswordExpires { get; set; }
        public DateTime? PasswordLastSet { get; set; }
        public DateTime? PasswordChangeableDate { get; set; }
        public DateTime? AccountExpires { get; set; }
        public DateTime? LastLogon { get; set; }
        public string HomeDirectory { get; set; } = string.Empty;
        public string Shell { get; set; } = string.Empty;
        public int UID { get; set; }
        public int GID { get; set; }
    }
}
