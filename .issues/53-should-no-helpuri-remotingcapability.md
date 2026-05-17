---
name: SHOULD — No HelpUri or RemotingCapability on any cmdlet
labels: [enhancement, SHOULD]
---

## Rule violated
- **Rule number:** Rule 12
- **Rule name:** Every cmdlet must declare HelpUri and RemotingCapability

## Location
- **Files:** All 15 cmdlets in `src/LocalAccounts.Linux.Native/Commands/`

## What's wrong
No `[Cmdlet]` attribute includes `HelpUri` or `RemotingCapability`. Rule 12 requires both.

Current:
```csharp
[Cmdlet(VerbsCommon.Get, "LocalUser", DefaultParameterSetName = "Default")]
```

Required:
```csharp
[Cmdlet(VerbsCommon.Get, "LocalUser", DefaultParameterSetName = "Default",
    HelpUri = "https://learn.microsoft.com/powershell/module/microsoft.powershell.localaccounts/get-localuser",
    RemotingCapability = RemotingCapability.SupportedByCommand)]
```

## How to fix
Add `HelpUri` (pointing to Microsoft Learn pages for Windows equivalents) and `RemotingCapability = RemotingCapability.SupportedByCommand` to all 15 `[Cmdlet]` attributes.

## Severity
- [ ] SHOULD — should be fixed before merge
