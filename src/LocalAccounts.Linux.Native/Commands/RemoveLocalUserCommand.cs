// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Management.Automation;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>Removes a local user account from a Linux system.</summary>
    [Cmdlet(VerbsCommon.Remove, "LocalUser", SupportsShouldProcess = true,
            ConfirmImpact = ConfirmImpact.High)]
    public sealed class RemoveLocalUserCommand : PSCmdlet
    {
        [Parameter(Mandatory = true, Position = 0, ValueFromPipeline = true,
                   ValueFromPipelineByPropertyName = true)]
        public string Name { get; set; } = string.Empty;

        [Parameter]
        public SwitchParameter RemoveHome { get; set; }

        protected override void ProcessRecord()
        {
            if (!ShouldProcess(Name, "Remove-LocalUser")) return;

            var args = RemoveHome ? new[] { "-r", Name } : new[] { Name };
            var (exit, _, stderr) = AccountHelpers.Run("userdel", args);
            if (exit != 0)
            {
                if (AccountHelpers.IsPermissionDenied(exit, stderr))
                {
                    WriteError(new ErrorRecord(
                        new InvalidOperationException("Remove-LocalUser requires root privileges."),
                        "ElevationRequired", ErrorCategory.PermissionDenied, Name));
                    return;
                }
                WriteError(new ErrorRecord(
                    new InvalidOperationException($"userdel failed (exit {exit}): {stderr.Trim()}"),
                    "UserdelFailed", ErrorCategory.InvalidOperation, Name));
            }
        }
    }
}
