// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Management.Automation;

namespace Microsoft.PowerShell.Commands;
    /// <summary>Modifies a local group on a Linux system.</summary>
    [Cmdlet(VerbsCommon.Set, "LocalGroup", SupportsShouldProcess = true)]
    public sealed class SetLocalGroupCommand : PSCmdlet
    {
        [Parameter(Mandatory = true, Position = 0, ValueFromPipeline = true,
                   ValueFromPipelineByPropertyName = true)]
        public string Name { get; set; } = string.Empty;

        [Parameter]
        public string? Description { get; set; }

        protected override void ProcessRecord()
        {
            if (!ShouldProcess(Name, "Set-LocalGroup")) return;

            var group = AccountHelpers.GetGroup(Name);
            if (group is null)
            {
                WriteError(new ErrorRecord(
                    new ItemNotFoundException($"Group '{Name}' was not found."),
                    "GroupNotFound", ErrorCategory.ObjectNotFound, Name));
                return;
            }

            if (Description is not null)
                WriteWarning("Set-LocalGroup: Linux groups do not support a description field. The Description parameter has no effect.");
        }
    }
