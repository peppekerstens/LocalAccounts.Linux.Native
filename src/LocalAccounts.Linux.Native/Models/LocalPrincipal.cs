// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.PowerShell.Commands
{
    /// <summary>Represents a member of a local group on a Linux system.</summary>
    public class LocalPrincipal
    {
        public string Name { get; set; } = string.Empty;
        public string ObjectClass { get; set; } = "User";
        public string PrincipalSource { get; set; } = "Local";
        public string? SID { get; set; }
    }
}
