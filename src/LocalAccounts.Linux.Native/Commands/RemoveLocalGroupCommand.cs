// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Management.Automation;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>Removes a local group from a Linux system.</summary>
    [Cmdlet(VerbsCommon.Remove, "LocalGroup", SupportsShouldProcess = true,
            ConfirmImpact = ConfirmImpact.High)]
    public sealed class RemoveLocalGroupCommand : PSCmdlet
    {
        [Parameter(Mandatory = true, Position = 0, ValueFromPipeline = true,
                   ValueFromPipelineByPropertyName = true)]
        public string Name { get; set; } = string.Empty;

        protected override void ProcessRecord()
        {
            if (!ShouldProcess(Name, "Remove-LocalGroup")) return;

            var (exit, _, stderr) = AccountHelpers.Run("groupdel", Name);
            if (exit != 0)
                WriteError(new ErrorRecord(
                    new InvalidOperationException($"groupdel failed (exit {exit}): {stderr.Trim()}"),
                    "GroupdelFailed", ErrorCategory.InvalidOperation, Name));
        }
    }
}
