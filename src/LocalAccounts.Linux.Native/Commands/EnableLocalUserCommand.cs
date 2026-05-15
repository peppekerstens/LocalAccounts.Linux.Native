// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Management.Automation;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>Enables a local user account on a Linux system.</summary>
    [Cmdlet(VerbsLifecycle.Enable, "LocalUser", SupportsShouldProcess = true)]
    public sealed class EnableLocalUserCommand : PSCmdlet
    {
        [Parameter(Mandatory = true, Position = 0, ValueFromPipeline = true,
                   ValueFromPipelineByPropertyName = true)]
        public string Name { get; set; } = string.Empty;

        protected override void ProcessRecord()
        {
            if (!ShouldProcess(Name, "Enable-LocalUser")) return;

            var (exit, _, stderr) = AccountHelpers.Run("usermod", "-U", Name);
            if (exit != 0)
            {
                if (AccountHelpers.IsPermissionDenied(exit, stderr))
                {
                    WriteError(new ErrorRecord(
                        new InvalidOperationException("Enable-LocalUser requires root privileges."),
                        "ElevationRequired", ErrorCategory.PermissionDenied, Name));
                    return;
                }
                WriteError(new ErrorRecord(
                    new InvalidOperationException($"usermod -U failed (exit {exit}): {stderr.Trim()}"),
                    "UsermodFailed", ErrorCategory.InvalidOperation, Name));
            }
        }
    }
}
