---
name: SHOULD — No #nullable enable on source files
labels: [enhancement, SHOULD]
---

## Rule violated
- **Rule number:** Rule 22
- **Rule name:** Nullable annotations must be consistent across all files

## Location
- **Files:** All `.cs` files in `src/LocalAccounts.Linux.Native/`

## What's wrong
No source file has `#nullable enable`. Rule 22 requires consistent nullable annotations.

## How to fix
Add `#nullable enable` at the top of each `.cs` file (after copyright header). Update nullable reference types accordingly (e.g., `string?` for nullable parameters).

## Severity
- [ ] SHOULD — should be fixed before merge
