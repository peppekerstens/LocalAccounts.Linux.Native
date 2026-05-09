// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Management.Automation;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>Creates a new local group on a Linux system.</summary>
    [Cmdlet(VerbsCommon.New, "LocalGroup", SupportsShouldProcess = true)]
    [OutputType(typeof(LocalGroup))]
    public sealed class NewLocalGroupCommand : PSCmdlet
    {
        [Parameter(Mandatory = true, Position = 0)]
        public string Name { get; set; } = string.Empty;

        [Parameter]
        public string? Description { get; set; }

        protected override void ProcessRecord()
        {
            if (!ShouldProcess(Name, "New-LocalGroup")) return;

            var (exit, _, stderr) = AccountHelpers.Run("groupadd", Name);
            if (exit != 0)
            {
                WriteError(new ErrorRecord(
                    new InvalidOperationException($"groupadd failed (exit {exit}): {stderr.Trim()}"),
                    "GroupaddFailed", ErrorCategory.InvalidOperation, Name));
                return;
            }

            var group = AccountHelpers.GetGroup(Name);
            if (group is not null) WriteObject(group);
        }
    }
}
