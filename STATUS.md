# LocalAccounts.Linux.Native — Module Status

**Last updated:** 2026-05-16
**Version:** 1.0.0
**GHA Build:** ✅ green
**GHA Pester:** ✅ green (5-distro + Windows)

---

## Current State

15 cmdlets implemented via P/Invoke libc. All write cmdlets translate elevation errors to `"CmdletName requires root privileges."`

### Output Types

| Type | Inherits | Windows Counterpart | Rule 9 Status |
|---|---|---|---|
| `LocalUser` | `LocalPrincipal` | `LocalUser : LocalPrincipal` | ✅ Compliant |
| `LocalGroup` | `LocalPrincipal` | `LocalGroup : LocalPrincipal` | ✅ Compliant |
| `LocalPrincipal` | `object` | `LocalPrincipal : object` | ✅ Compliant |

### Rule 9 Compliance

**Fixed (2026-05-16, commit `3f6da2b`):**
- `LocalUser` and `LocalGroup` now inherit from `LocalPrincipal` (matches Windows)
- `SID` type changed from `string?` to `SecurityIdentifier?`
- `PrincipalSource` type changed from `string` to `PrincipalSource?` enum
- Added `ToString()` override on `LocalPrincipal`, `LocalUser`, `LocalGroup`
- Added `Clone()` methods on `LocalUser` and `LocalGroup`
- Created `PrincipalSource` enum matching Windows `System.Security.Principal.PrincipalSource`
- Added `System.Security.Principal.Windows` 5.0.0 NuGet package reference

**GHA:** ✅ Build green, ✅ Pester green (5-distro + Windows)

---

## Known Issues

| Issue | Severity | Status |
|---|---|---|
| `Remove-LocalUser`/`Remove-LocalGroup` return "user/group does not exist" on nonexistent targets instead of elevation error | ℹ️ Expected | Can't test removal elevation without root-created target |

**Tracked on GitHub:** [Issues #1–#7](https://github.com/peppekerstens/LocalAccounts.Linux.Native/issues) (3 MUST, 4 SHOULD from code audit)

## Next Steps

1. Continue upstream preparation — type alignment complete for this module

---

## Reference

| Resource | Location |
|---|---|
| Source code | `src/LocalAccounts.Linux.Native/` |
| Tests | `tests/LocalAccounts.Linux.Native.Tests/` |
| Linux rules | `docs/linux-rules.md` |
| Coordination repo | `https://github.com/peppekerstens/opencode` |
