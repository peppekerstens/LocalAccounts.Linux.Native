// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Management.Automation;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>Disables a local user account on a Linux system.</summary>
    [Cmdlet(VerbsLifecycle.Disable, "LocalUser", SupportsShouldProcess = true)]
    public sealed class DisableLocalUserCommand : PSCmdlet
    {
        [Parameter(Mandatory = true, Position = 0, ValueFromPipeline = true,
                   ValueFromPipelineByPropertyName = true)]
        public string Name { get; set; } = string.Empty;

        protected override void ProcessRecord()
        {
            if (!ShouldProcess(Name, "Disable-LocalUser")) return;

            var (exit, _, stderr) = AccountHelpers.Run("usermod", "-L", Name);
            if (exit != 0)
            {
                if (AccountHelpers.IsPermissionDenied(exit, stderr))
                {
                    WriteError(new ErrorRecord(
                        new InvalidOperationException("Disable-LocalUser requires root privileges."),
                        "ElevationRequired", ErrorCategory.PermissionDenied, Name));
                    return;
                }
                WriteError(new ErrorRecord(
                    new InvalidOperationException($"usermod -L failed (exit {exit}): {stderr.Trim()}"),
                    "UsermodFailed", ErrorCategory.InvalidOperation, Name));
            }
        }
    }
}
