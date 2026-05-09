// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.PowerShell.Commands
{
    /// <summary>Represents a local group on a Linux system.</summary>
    public class LocalGroup
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? SID { get; set; }
        public string ObjectClass { get; set; } = "Group";
        public string PrincipalSource { get; set; } = "Local";
        public int GID { get; set; }
    }
}
