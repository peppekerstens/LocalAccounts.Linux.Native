// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Management.Automation;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>Renames a local group on a Linux system.</summary>
    [Cmdlet(VerbsCommon.Rename, "LocalGroup", SupportsShouldProcess = true)]
    [OutputType(typeof(LocalGroup))]
    public sealed class RenameLocalGroupCommand : PSCmdlet
    {
        [Parameter(Mandatory = true, Position = 0, ValueFromPipelineByPropertyName = true)]
        public string Name { get; set; } = string.Empty;

        [Parameter(Mandatory = true, Position = 1)]
        public string NewName { get; set; } = string.Empty;

        protected override void ProcessRecord()
        {
            if (!ShouldProcess($"{Name} -> {NewName}", "Rename-LocalGroup")) return;

            var (exit, _, stderr) = AccountHelpers.Run("groupmod", "-n", NewName, Name);
            if (exit != 0)
            {
                WriteError(new ErrorRecord(
                    new InvalidOperationException($"groupmod -n failed (exit {exit}): {stderr.Trim()}"),
                    "GroupmodFailed", ErrorCategory.InvalidOperation, Name));
                return;
            }

            var group = AccountHelpers.GetGroup(NewName);
            if (group is not null) WriteObject(group);
        }
    }
}
