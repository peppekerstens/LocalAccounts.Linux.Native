// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Management.Automation;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>Gets members of a local group on a Linux system.</summary>
    [Cmdlet(VerbsCommon.Get, "LocalGroupMember")]
    [OutputType(typeof(LocalPrincipal))]
    public sealed class GetLocalGroupMemberCommand : PSCmdlet
    {
        [Parameter(Mandatory = true, Position = 0, ValueFromPipeline = true,
                   ValueFromPipelineByPropertyName = true)]
        public string Group { get; set; } = string.Empty;

        protected override void ProcessRecord()
        {
            // Validate group exists
            if (AccountHelpers.GetGroup(Group) is null)
            {
                WriteError(new ErrorRecord(
                    new ItemNotFoundException($"Group '{Group}' was not found."),
                    "GroupNotFound", ErrorCategory.ObjectNotFound, Group));
                return;
            }

            foreach (var member in AccountHelpers.GetGroupMembers(Group))
                WriteObject(member);
        }
    }
}
