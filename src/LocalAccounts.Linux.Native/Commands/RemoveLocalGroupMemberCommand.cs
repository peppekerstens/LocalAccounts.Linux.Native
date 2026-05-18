// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Management.Automation;

namespace Microsoft.PowerShell.Commands;
    /// <summary>Removes a user from a local group on a Linux system.</summary>
    [Cmdlet(VerbsCommon.Remove, "LocalGroupMember", SupportsShouldProcess = true,
            ConfirmImpact = ConfirmImpact.Medium)]
    public sealed class RemoveLocalGroupMemberCommand : PSCmdlet
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
                if (!ShouldProcess($"{m} from {Group}", "Remove-LocalGroupMember")) continue;

                var (exit, _, stderr) = AccountHelpers.Run("gpasswd", "-d", m, Group);
                if (exit != 0)
                {
                    if (AccountHelpers.IsPermissionDenied(exit, stderr))
                    {
                        WriteError(new ErrorRecord(
                            new InvalidOperationException("Remove-LocalGroupMember requires root privileges."),
                            "ElevationRequired", ErrorCategory.PermissionDenied, m));
                        continue;
                    }
                    WriteError(new ErrorRecord(
                        new InvalidOperationException($"gpasswd -d failed (exit {exit}): {stderr.Trim()}"),
                        "GpasswdFailed", ErrorCategory.InvalidOperation, m));
                }
            }
        }
    }
