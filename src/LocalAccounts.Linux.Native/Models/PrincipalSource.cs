// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.PowerShell.Commands;

/// <summary>
/// Specifies the source of a security principal.
/// </summary>
public enum PrincipalSource
{
    /// <summary>Unknown source.</summary>
    Unknown = 0,
    /// <summary>Local machine.</summary>
    Local = 1,
    /// <summary>Active Directory.</summary>
    ActiveDirectory = 2,
    /// <summary>Azure Active Directory.</summary>
    AzureAD = 3,
    /// <summary>Microsoft Account.</summary>
    MicrosoftAccount = 4,
}
