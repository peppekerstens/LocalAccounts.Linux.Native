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
| `LocalUser` | `object` | `LocalUser : LocalPrincipal` | ⬜ Non-compliant |
| `LocalGroup` | `object` | `LocalGroup : LocalPrincipal` | ⬜ Non-compliant |
| `LocalPrincipal` | `object` | `LocalPrincipal : object` | ⬜ Non-compliant |

### Rule 9 Compliance Gaps

**Critical:**
- `LocalUser` and `LocalGroup` do not inherit from `LocalPrincipal` (Windows: they do)
- `SID` is `string?` (Windows: `SecurityIdentifier`)
- `PrincipalSource` is `string` (Windows: `PrincipalSource` enum)
- Missing `ToString()` override on `LocalPrincipal`
- Missing `Clone()` methods on `LocalUser` and `LocalGroup`

**Planned fixes:**
1. Create `PrincipalSource` enum matching Windows
2. Change `LocalPrincipal.SID` to `SecurityIdentifier?`
3. Change `LocalPrincipal.PrincipalSource` to `PrincipalSource?`
4. Make `LocalUser : LocalPrincipal` and `LocalGroup : LocalPrincipal`
5. Add `ToString()` and `Clone()` methods

---

## Known Issues

| Issue | Severity | Status |
|---|---|---|
| `Remove-LocalUser`/`Remove-LocalGroup` return "user/group does not exist" on nonexistent targets instead of elevation error | ℹ️ Expected | Can't test removal elevation without root-created target |

## Next Steps

1. Fix Rule 9 compliance gaps (inheritance chain, type mismatches)
2. Update all cmdlet constructors to use new type hierarchy
3. Add Pester tests for new type properties
4. Trigger GHA pester workflow — verify all green

---

## Reference

| Resource | Location |
|---|---|
| Source code | `src/LocalAccounts.Linux.Native/` |
| Tests | `tests/LocalAccounts.Linux.Native.Tests/` |
| Linux rules | `docs/linux-rules.md` |
| Coordination repo | `https://github.com/peppekerstens/opencode` |
