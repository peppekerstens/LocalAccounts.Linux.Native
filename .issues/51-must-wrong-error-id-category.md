---
name: MUST — Error ID and category don't match Rule 6
labels: [bug, MUST]
---

## Rule violated
- **Rule number:** Rule 6
- **Rule name:** Consistent error handling across all modules

## Location
- **File:** `src/LocalAccounts.Linux.Native/Commands/NewLocalUserCommand.cs`, lines 68-71
- **File:** All other write cmdlets with elevation error handling

## What's wrong
Elevation errors use `"ElevationRequired"` as error ID and `ErrorCategory.PermissionDenied` instead of `"UnauthorizedAccess"` and `ErrorCategory.SecurityError` as required by Rule 6.

```csharp
WriteError(new ErrorRecord(
    new InvalidOperationException("New-LocalUser requires root privileges."),
    "ElevationRequired", ErrorCategory.PermissionDenied, Name));  // ❌ wrong
```

## How to fix
Change to:
```csharp
WriteError(new ErrorRecord(
    new PSSecurityException($"{MyInvocation.MyCommand.Name} requires root privileges."),
    "UnauthorizedAccess", ErrorCategory.SecurityError, Name));
```

## Severity
- [x] MUST — blocks merge
