// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Management.Automation;

namespace Microsoft.PowerShell.Commands;
    /// <summary>Modifies a local user account on a Linux system.</summary>
    [Cmdlet(VerbsCommon.Set, "LocalUser", SupportsShouldProcess = true)]
    public sealed class SetLocalUserCommand : PSCmdlet
    {
        [Parameter(Mandatory = true, Position = 0, ValueFromPipeline = true,
                   ValueFromPipelineByPropertyName = true)]
        public string Name { get; set; } = string.Empty;

        [Parameter] public string? FullName { get; set; }
        [Parameter] public string? Description { get; set; }
        [Parameter] public System.Security.SecureString? Password { get; set; }
        [Parameter] public DateTime? AccountExpires { get; set; }
        [Parameter] public SwitchParameter AccountNeverExpires { get; set; }
        [Parameter] public string? Shell { get; set; }
        [Parameter] public string? HomeDirectory { get; set; }
        [Parameter] public SwitchParameter PasswordNeverExpires { get; set; }
        [Parameter] public bool? UserMayChangePassword { get; set; }

        protected override void ProcessRecord()
        {
            if (!ShouldProcess(Name, "Set-LocalUser")) return;

            var args = new List<string>();

            string? gecos = FullName ?? Description;
            if (gecos is not null) { args.Add("--comment"); args.Add(gecos); }
            if (Shell is not null) { args.Add("--shell"); args.Add(Shell); }
            if (HomeDirectory is not null) { args.Add("--home"); args.Add(HomeDirectory); }

            if (AccountNeverExpires)
            {
                args.Add("--expiredate"); args.Add(string.Empty);
            }
            else if (AccountExpires.HasValue)
            {
                args.Add("--expiredate");
                args.Add(AccountExpires.Value.ToString("yyyy-MM-dd"));
            }

            if (args.Count > 0)
            {
                args.Add(Name);
                var (exit, _, stderr) = AccountHelpers.Run("usermod", args.ToArray());
                if (exit != 0)
                {
                    if (AccountHelpers.IsPermissionDenied(exit, stderr))
                    {
                        WriteError(new ErrorRecord(
                            new InvalidOperationException("Set-LocalUser requires root privileges."),
                            "ElevationRequired", ErrorCategory.PermissionDenied, Name));
                        return;
                    }
                    WriteError(new ErrorRecord(
                        new InvalidOperationException($"usermod failed (exit {exit}): {stderr.Trim()}"),
                        "UsermodFailed", ErrorCategory.InvalidOperation, Name));
                    return;
                }
            }

            if (Password is not null)
                AccountHelpers.SetPassword(Name, Password);

            if (PasswordNeverExpires)
                AccountHelpers.Run("chage", "-M", "99999", Name);
        }
    }
