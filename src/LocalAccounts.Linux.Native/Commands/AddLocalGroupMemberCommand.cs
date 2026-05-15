// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Management.Automation;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>Adds a user to a local group on a Linux system.</summary>
    [Cmdlet(VerbsCommon.Add, "LocalGroupMember", SupportsShouldProcess = true)]
    public sealed class AddLocalGroupMemberCommand : PSCmdlet
    {
        [Parameter(Mandatory = true, Position = 0)]
        public string Group { get; set; } = string.Empty;

        [Parameter(Mandatory = true, Position = 1, ValueFromPipeline = true,
                   ValueFromPipelineByPropertyName = true)]
        public string[] Member { get; set; } = Array.Empty<string>();

        protected override void ProcessRecord()
        {
            foreach (var m in Member)
            {
                if (!ShouldProcess($"{m} -> {Group}", "Add-LocalGroupMember")) continue;

                var (exit, _, stderr) = AccountHelpers.Run("usermod", "-aG", Group, m);
                if (exit != 0)
                {
                    if (AccountHelpers.IsPermissionDenied(exit, stderr))
                    {
                        WriteError(new ErrorRecord(
                            new InvalidOperationException("Add-LocalGroupMember requires root privileges."),
                            "ElevationRequired", ErrorCategory.PermissionDenied, m));
                        continue;
                    }
                    WriteError(new ErrorRecord(
                        new InvalidOperationException($"usermod -aG failed (exit {exit}): {stderr.Trim()}"),
                        "UsermodFailed", ErrorCategory.InvalidOperation, m));
                }
            }
        }
    }
}
