---
name: SHOULD — No shared base class for cmdlets
labels: [enhancement, SHOULD]
---

## Rule violated
- **Rule number:** Rule 21
- **Rule name:** Shared logic must use base classes, not copy-paste

## Location
- **Files:** All 15 cmdlets in `src/LocalAccounts.Linux.Native/Commands/`

## What's wrong
Each cmdlet is a standalone class with duplicated parameter definitions (Name, ValidateNotNullOrEmpty, etc.) and platform branching logic. Rule 21 requires: "Cmdlets that share parameters or logic must inherit from a common abstract base class."

## How to fix
Create a `LocalAccountBase : PSCmdlet` abstract class with shared parameters and `ProcessRecord()` platform branching. Individual cmdlets inherit from it and implement `OperateOnAccount()`.

## Severity
- [ ] SHOULD — should be fixed before merge
