// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Management.Automation;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>Gets local groups on a Linux system.</summary>
    [Cmdlet(VerbsCommon.Get, "LocalGroup", DefaultParameterSetName = "Default")]
    [OutputType(typeof(LocalGroup))]
    public sealed class GetLocalGroupCommand : PSCmdlet
    {
        [Parameter(Position = 0, ValueFromPipeline = true,
                   ValueFromPipelineByPropertyName = true)]
        [SupportsWildcards]
        public string[]? Name { get; set; }

        protected override void ProcessRecord()
        {
            var patterns = Name is { Length: > 0 } ? Name : new[] { "*" };

            foreach (var group in AccountHelpers.GetAllGroups())
            {
                foreach (var pat in patterns)
                {
                    if (WildcardPattern.ContainsWildcardCharacters(pat)
                            ? new WildcardPattern(pat, WildcardOptions.IgnoreCase).IsMatch(group.Name)
                            : string.Equals(group.Name, pat, StringComparison.OrdinalIgnoreCase))
                    {
                        WriteObject(group);
                        break;
                    }
                }
            }
        }
    }
}
