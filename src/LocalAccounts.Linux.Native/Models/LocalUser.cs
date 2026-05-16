// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.PowerShell.Commands;

/// <summary>Represents a local user account on a Linux system.</summary>
public class LocalUser : LocalPrincipal
{
    public string FullName { get; internal set; } = string.Empty;
    public string Description { get; internal set; } = string.Empty;
    public bool Enabled { get; internal set; }
    public bool PasswordRequired { get; internal set; }
    public bool UserMayChangePassword { get; internal set; } = true;
    public DateTime? PasswordExpires { get; internal set; }
    public DateTime? PasswordLastSet { get; internal set; }
    public DateTime? PasswordChangeableDate { get; internal set; }
    public DateTime? AccountExpires { get; internal set; }
    public DateTime? LastLogon { get; internal set; }
    public string HomeDirectory { get; internal set; } = string.Empty;
    public string Shell { get; internal set; } = string.Empty;
    public int UID { get; internal set; }
    public int GID { get; internal set; }

    /// <summary>Creates a shallow copy of this instance.</summary>
    public LocalUser Clone() => (LocalUser)MemberwiseClone();

    /// <inheritdoc/>
    public override string ToString() => Name ?? string.Empty;
}
