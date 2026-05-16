// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.PowerShell.Commands;

/// <summary>Represents a local group on a Linux system.</summary>
public class LocalGroup : LocalPrincipal
{
    public string Description { get; internal set; } = string.Empty;
    public int GID { get; internal set; }

    /// <summary>Creates a shallow copy of this instance.</summary>
    public LocalGroup Clone() => (LocalGroup)MemberwiseClone();

    /// <inheritdoc/>
    public override string ToString() => Name ?? string.Empty;
}
