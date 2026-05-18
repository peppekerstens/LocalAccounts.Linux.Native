// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Management.Automation;
using System.Text.RegularExpressions;

namespace Microsoft.PowerShell.Commands;
    /// <summary>Gets local user accounts on a Linux system.</summary>
    [Cmdlet(VerbsCommon.Get, "LocalUser", DefaultParameterSetName = "Default")]
    [OutputType(typeof(LocalUser))]
    public sealed class GetLocalUserCommand : PSCmdlet
    {
        [Parameter(Position = 0, ValueFromPipeline = true,
                   ValueFromPipelineByPropertyName = true)]
        [SupportsWildcards]
        public string[]? Name { get; set; }

        protected override void ProcessRecord()
        {
            var patterns = Name is { Length: > 0 }
                ? Name
                : new[] { "*" };

            foreach (var user in AccountHelpers.GetAllUsers())
            {
                foreach (var pat in patterns)
                {
                    if (WildcardPattern.ContainsWildcardCharacters(pat)
                            ? new WildcardPattern(pat, WildcardOptions.IgnoreCase).IsMatch(user.Name)
                            : string.Equals(user.Name, pat, StringComparison.OrdinalIgnoreCase))
                    {
                        WriteObject(user);
                        break;
                    }
                }
            }
        }
    }
