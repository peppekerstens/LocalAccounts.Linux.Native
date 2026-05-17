---
name: MUST — No OperatingSystem.IsWindows() guard on any cmdlet
labels: [bug, MUST]
---

## Rule violated
- **Rule number:** Rule 8
- **Rule name:** Platform branching at the top of ProcessRecord()

## Location
- **Files:** All 15 cmdlets in `src/LocalAccounts.Linux.Native/Commands/`

## What's wrong
No cmdlet has `OperatingSystem.IsWindows()` branching. Rule 8 requires: "C# cmdlets that have Windows counterparts must branch immediately." This prevents infinite recursion when the module name matches a Windows built-in cmdlet.

## How to fix
Add at the top of each `ProcessRecord()`:
```csharp
if (OperatingSystem.IsWindows())
{
    string cmdletName = MyInvocation.MyCommand.Name;
    InvokeCommand.InvokeScript($"Microsoft.PowerShell.LocalAccounts\\{cmdletName}");
    return;
}
```

## Severity
- [x] MUST — blocks merge
