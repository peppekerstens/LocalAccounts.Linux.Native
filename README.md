# LocalAccounts.Linux.Native

[![Pester Tests](https://github.com/peppekerstens/LocalAccounts.Linux.Native/actions/workflows/pester.yml/badge.svg)](https://github.com/peppekerstens/LocalAccounts.Linux.Native/actions/workflows/pester.yml)

> Native C# binary module implementing the full 15-cmdlet `*-LocalUser`, `*-LocalGroup`, and `*-LocalGroupMember` surface of `Microsoft.PowerShell.LocalAccounts` for Linux.

This is the Tier 2 (C# native) successor to [`PowerShell.LocalAccounts.Linux`](https://github.com/peppekerstens/PowerShell.LocalAccounts.Linux), part of Stage 5 of the [PowerShell Linux Commands](https://peppekerstens.github.io) project.

---

## What it does

Provides all 15 cmdlets from `Microsoft.PowerShell.LocalAccounts` as a compiled binary module. Read operations (`Get-*`) use P/Invoke directly into `libc` (`getpwent`, `getgrent`, `getspnam`) — zero subprocesses for enumeration. Write operations use the standard Linux user management tools.

| Cmdlet | Backend | Notes |
|---|---|---|
| `Get-LocalUser` | P/Invoke `getpwent` / `getspnam` | Full object: UID, shell, home, enabled state, expiry |
| `Get-LocalGroup` | P/Invoke `getgrent` | Includes GID and member list |
| `Get-LocalGroupMember` | P/Invoke `getpwent` + `getgrent` | Includes primary-group members |
| `New-LocalUser` | `useradd`, `chpasswd`, `usermod` | SupportsShouldProcess |
| `New-LocalGroup` | `groupadd` | SupportsShouldProcess |
| `Set-LocalUser` | `usermod`, `chage`, `chpasswd` | SupportsShouldProcess |
| `Set-LocalGroup` | `getent group` (validate only) | No-op — Linux groups have no description field; warns |
| `Enable-LocalUser` | `usermod -U` | Unlocks password hash |
| `Disable-LocalUser` | `chpasswd` + `usermod -L` | Sets locked hash first for passwordless accounts |
| `Remove-LocalUser` | `userdel` | `-RemoveHome` to also delete home directory |
| `Remove-LocalGroup` | `groupdel` | SupportsShouldProcess |
| `Add-LocalGroupMember` | `usermod -aG` | SupportsShouldProcess |
| `Remove-LocalGroupMember` | `gpasswd -d` | SupportsShouldProcess |
| `Rename-LocalUser` | `usermod -l` | `-MoveHome` to also rename home directory |
| `Rename-LocalGroup` | `groupmod -n` | SupportsShouldProcess |

All write cmdlets support `-WhatIf` and `-Confirm`.

---

## Requirements

- Linux only
- PowerShell 7.4+, .NET 8
- Standard Linux utilities: `useradd`, `usermod`, `userdel`, `groupadd`, `groupmod`, `groupdel`, `gpasswd`, `chpasswd`, `chage`
- Most write operations require root or `sudo`

---

## Installation

```powershell
git clone https://github.com/peppekerstens/LocalAccounts.Linux.Native
dotnet build LocalAccounts.Linux.Native/src/LocalAccounts.Linux.Native --configuration Release
Import-Module ./LocalAccounts.Linux.Native/src/LocalAccounts.Linux.Native/bin/Release/net8.0/LocalAccounts.Linux.Native.dll
```

---

## Usage

```powershell
# List all users
Get-LocalUser

# Find locked accounts
Get-LocalUser | Where-Object { -not $_.Enabled }

# Show group membership
Get-LocalGroupMember -Group sudo

# Create a new user
New-LocalUser -Name alice -FullName 'Alice Smith' -Password (Read-Host -AsSecureString)

# Add user to group
Add-LocalGroupMember -Group sudo -Member alice

# Disable an account
Disable-LocalUser -Name alice

# Remove a user and their home directory
Remove-LocalUser -Name alice -RemoveHome
```

---

## CI / Testing

Tested across 5 Linux distributions in containers on every push:

| Distro | Image |
|---|---|
| Ubuntu 22.04 | `peppekerstens/testinfra:ubuntu2204` |
| Ubuntu 24.04 | `peppekerstens/testinfra:ubuntu2404` |
| Debian 12 | `peppekerstens/testinfra:debian12` |
| Fedora 41 | `peppekerstens/testinfra:fedora41` |
| openSUSE Tumbleweed | `peppekerstens/testinfra:opensuse-tumbleweed` |

### Test scenarios

| Describe block | Scope | Tests |
|---|---|---|
| Module surface | everywhere | 15 cmdlet export checks |
| Get-LocalUser read ops | Linux (any user) | Enumerate, filter by name, wildcard, nonexistent |
| Get-LocalGroup read ops | Linux (any user) | Enumerate, filter, nonexistent |
| Get-LocalGroupMember read ops | Linux (any user) | Returns members; primary-group edge case (root in root) |
| WhatIf safety | everywhere | All write cmdlets with -WhatIf |
| 10-user write lifecycle | Linux + root | New/Set/Rename/Disable/Enable/Remove ×10 |
| 2-group write lifecycle | Linux + root | New/Rename/Add-Member/Remove-Member/Remove ×2 |
| E2E lifecycle | Linux + root | Full user+group create → modify → membership → remove cycle |
| Service account scenario | Linux + root | Create with nologin shell; lock; primary-group membership; remove |
| Operator bulk membership | Linux + root | Bulk Add-LocalGroupMember; bulk Disable/Enable/Remove via pipeline |
| Account expiry scenario | Linux + root | Set-LocalUser -AccountExpires; -AccountNeverExpires |

Run locally (requires Docker):

```powershell
Invoke-Pester -Path tests/LocalAccounts.Linux.Native.Tests/ -Output Detailed
```

---

## Implementation Notes

- **P/Invoke reads**: `getpwent`/`getgrent` enumerate passwd/group entries directly via libc — no `getent` subprocess needed.
- **`getspnam` degrades gracefully for non-root callers**: shadow password fields default to safe values; `Enabled` defaults to `$true` with a warning.
- **`Disable-LocalUser` on passwordless accounts**: `usermod -L` exits 6 if there is no password hash to lock. The module sets a locked hash via `chpasswd` first, then locks.
- **Type compatibility**: Output objects (`LocalUser`, `LocalGroup`, `LocalPrincipal`) live in the `Microsoft.PowerShell.Commands` namespace, matching the Windows module's type names for script compatibility.
- **SIDs**: Linux has no SIDs. All SID properties return `$null`.
- **Primary-group membership**: `Get-LocalGroupMember` includes users whose primary GID matches the group, not just those listed explicitly in `/etc/group`.

---

## Version history

| Version | Changes |
|---|---|
| 0.1.0 | Initial release. All 15 cmdlets. P/Invoke reads via libc. 116 Pester tests. |
| 0.2.0 | Test expansion. Service account scenario (nologin shell, lock, remove); operator bulk membership (pipeline disable/enable/remove); account expiry (`Set-LocalUser -AccountExpires/-AccountNeverExpires`); primary-group edge case (`root` in `root` group). ~140+ tests. |

---

## Related

- [`PowerShell.LocalAccounts.Linux`](https://github.com/peppekerstens/PowerShell.LocalAccounts.Linux) — the Stage 1 PowerShell script wrapper this module replaces
- [opencode project plan](https://github.com/peppekerstens/opencode) — multi-stage project tracking
- [Blog series](https://peppekerstens.github.io) — write-up of the full journey

---

## License

[GNU General Public License v3](LICENSE)
