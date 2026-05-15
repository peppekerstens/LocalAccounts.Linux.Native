// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Management.Automation;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>Creates a new local user account on a Linux system.</summary>
    [Cmdlet(VerbsCommon.New, "LocalUser", DefaultParameterSetName = "Password",
            SupportsShouldProcess = true)]
    [OutputType(typeof(LocalUser))]
    public sealed class NewLocalUserCommand : PSCmdlet
    {
        [Parameter(Mandatory = true, Position = 0)]
        public string Name { get; set; } = string.Empty;

        [Parameter]
        public string? FullName { get; set; }

        [Parameter]
        public string? Description { get; set; }

        [Parameter(ParameterSetName = "Password")]
        public System.Security.SecureString? Password { get; set; }

        [Parameter(ParameterSetName = "NoPassword")]
        public SwitchParameter NoPassword { get; set; }

        [Parameter]
        public DateTime? AccountExpires { get; set; }

        [Parameter]
        public SwitchParameter AccountNeverExpires { get; set; }

        [Parameter]
        public string? HomeDirectory { get; set; }

        [Parameter]
        public string Shell { get; set; } = "/bin/bash";

        [Parameter]
        public SwitchParameter Disabled { get; set; }

        protected override void ProcessRecord()
        {
            if (!ShouldProcess(Name, "New-LocalUser")) return;

            var args = new List<string> { "--shell", Shell, "--create-home" };

            string gecos = FullName ?? Description ?? string.Empty;
            if (!string.IsNullOrEmpty(gecos)) { args.Add("--comment"); args.Add(gecos); }

            if (HomeDirectory is not null) { args.Add("--home-dir"); args.Add(HomeDirectory); }

            if (AccountExpires.HasValue && !AccountNeverExpires)
            {
                args.Add("--expiredate");
                args.Add(AccountExpires.Value.ToString("yyyy-MM-dd"));
            }

            args.Add(Name);

            var (exit, _, stderr) = AccountHelpers.Run("useradd", args.ToArray());
            if (exit != 0)
            {
                if (AccountHelpers.IsPermissionDenied(exit, stderr))
                {
                    WriteError(new ErrorRecord(
                        new InvalidOperationException("New-LocalUser requires root privileges."),
                        "ElevationRequired", ErrorCategory.PermissionDenied, Name));
                    return;
                }
                WriteError(new ErrorRecord(
                    new InvalidOperationException($"useradd failed (exit {exit}): {stderr.Trim()}"),
                    "UserAddFailed", ErrorCategory.InvalidOperation, Name));
                return;
            }

            if (Password is not null)
                AccountHelpers.SetPassword(Name, Password);
            else if (NoPassword)
                AccountHelpers.Run("passwd", "-d", Name);

            if (Disabled)
                AccountHelpers.Run("usermod", "-L", Name);

            var user = AccountHelpers.GetUser(Name);
            if (user is not null) WriteObject(user);
        }
    }
}
