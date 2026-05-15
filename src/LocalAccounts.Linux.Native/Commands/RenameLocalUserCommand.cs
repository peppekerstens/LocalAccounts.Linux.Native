// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Management.Automation;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>Renames a local user account on a Linux system.</summary>
    [Cmdlet(VerbsCommon.Rename, "LocalUser", SupportsShouldProcess = true)]
    [OutputType(typeof(LocalUser))]
    public sealed class RenameLocalUserCommand : PSCmdlet
    {
        [Parameter(Mandatory = true, Position = 0, ValueFromPipelineByPropertyName = true)]
        public string Name { get; set; } = string.Empty;

        [Parameter(Mandatory = true, Position = 1)]
        public string NewName { get; set; } = string.Empty;

        [Parameter]
        public SwitchParameter MoveHome { get; set; }

        protected override void ProcessRecord()
        {
            if (!ShouldProcess($"{Name} -> {NewName}", "Rename-LocalUser")) return;

            var args = new List<string> { "-l", NewName };
            if (MoveHome) { args.Add("-m"); args.Add("-d"); args.Add($"/home/{NewName}"); }
            args.Add(Name);

            var (exit, _, stderr) = AccountHelpers.Run("usermod", args.ToArray());
            if (exit != 0)
            {
                if (AccountHelpers.IsPermissionDenied(exit, stderr))
                {
                    WriteError(new ErrorRecord(
                        new InvalidOperationException("Rename-LocalUser requires root privileges."),
                        "ElevationRequired", ErrorCategory.PermissionDenied, Name));
                    return;
                }
                WriteError(new ErrorRecord(
                    new InvalidOperationException($"usermod -l failed (exit {exit}): {stderr.Trim()}"),
                    "UsermodFailed", ErrorCategory.InvalidOperation, Name));
                return;
            }

            var user = AccountHelpers.GetUser(NewName);
            if (user is not null) WriteObject(user);
        }
    }
}
