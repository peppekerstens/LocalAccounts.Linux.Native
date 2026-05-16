// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Security.Principal;

namespace Microsoft.PowerShell.Commands;

/// <summary>Represents a member of a local group on a Linux system.</summary>
public class LocalPrincipal
{
    /// <summary>Parameterless constructor for derived classes and object initializers.</summary>
    public LocalPrincipal() { }

    public string Name { get; internal set; } = string.Empty;
    public string ObjectClass { get; internal set; } = string.Empty;
    public PrincipalSource? PrincipalSource { get; internal set; }
    public SecurityIdentifier? SID { get; internal set; }

    /// <inheritdoc/>
#pragma warning disable CA1416 // SecurityIdentifier.Value is Windows-only but ToString() is cross-platform
    public override string ToString() => Name ?? SID?.Value ?? string.Empty;
#pragma warning restore CA1416
}
