---
name: MUST — No proactive elevation check on write cmdlets
labels: [bug, MUST]
---

## Rule violated
- **Rule number:** Rule 1
- **Rule name:** Elevation checks are mandatory for write operations

## Location
- **Files:** All write cmdlets in `src/LocalAccounts.Linux.Native/Commands/` (NewLocalUser, SetLocalUser, RemoveLocalUser, EnableLocalUser, DisableLocalUser, RenameLocalUser, NewLocalGroup, SetLocalGroup, RemoveLocalGroup, RenameLocalGroup, AddLocalGroupMember, RemoveLocalGroupMember)

## What's wrong
Write cmdlets rely on reactive subprocess error translation instead of proactively checking elevation before acting. Rule 1 requires: "Every cmdlet that changes system state must check elevation before acting."

Current pattern in `NewLocalUserCommand.cs`:
```csharp
var (exit, _, stderr) = AccountHelpers.Run("useradd", args.ToArray());
if (exit != 0)
{
    if (AccountHelpers.IsPermissionDenied(exit, stderr))
    {
        WriteError(new ErrorRecord(
            new InvalidOperationException("New-LocalUser requires root privileges."),
            "ElevationRequired", ErrorCategory.PermissionDenied, Name));
    }
}
```

Required pattern:
```csharp
if (!Utils.IsAdministrator())
{
    throw new PSSecurityException($"{MyInvocation.MyCommand.Name} requires root privileges.");
}
```

## How to fix
Add `Utils.IsAdministrator()` check at the start of each write cmdlet's `ProcessRecord()` method, before any subprocess call.

## Severity
- [x] MUST — blocks merge
