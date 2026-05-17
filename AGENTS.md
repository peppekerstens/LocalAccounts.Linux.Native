# LocalAccounts.Linux.Native — Contributor Guide

## What this module is

A C# binary PowerShell module providing Linux `*-LocalUser` and `*-LocalGroup` cmdlets via P/Invoke to libc (`getpwnam`, `getgrnam`, `setpwent`, etc.). 15 cmdlets total. Designed as a drop-in replacement for Windows `Microsoft.PowerShell.LocalAccounts` on Linux.

Part of the [PowerShell Linux Commands](https://github.com/peppekerstens/opencode) project.

---

## Quick Start

```bash
# Build
dotnet build -c Release

# Run tests (requires pwsh)
pwsh -c "Import-Module ./src/LocalAccounts.Linux.Native/bin/Release/net8.0/LocalAccounts.Linux.Native.dll; Invoke-Pester ./tests/"
```

---

## Architecture

```
src/LocalAccounts.Linux.Native/
├── Commands/          # Cmdlet implementations (15 cmdlets)
│   ├── GetLocalUserCommand.cs
│   ├── NewLocalUserCommand.cs
│   ├── SetLocalUserCommand.cs
│   ├── RemoveLocalUserCommand.cs
│   ├── EnableLocalUserCommand.cs
│   ├── DisableLocalUserCommand.cs
│   ├── RenameLocalUserCommand.cs
│   ├── GetLocalGroupCommand.cs
│   ├── NewLocalGroupCommand.cs
│   ├── SetLocalGroupCommand.cs
│   ├── RemoveLocalGroupCommand.cs
│   ├── RenameLocalGroupCommand.cs
│   ├── AddLocalGroupMemberCommand.cs
│   ├── RemoveLocalGroupMemberCommand.cs
│   └── RenameLocalGroupMemberCommand.cs
├── Helpers/
│   └── AccountHelpers.cs     # Subprocess runner, elevation checks, locking
└── Models/
    ├── LocalPrincipal.cs     # Base class (matches Windows)
    ├── LocalUser.cs          # Inherits LocalPrincipal
    ├── LocalGroup.cs         # Inherits LocalPrincipal
    └── PrincipalSource.cs    # Enum matching Windows PrincipalSource
```

### Key design decisions

- **P/Invoke libc** — Direct calls to `getpwnam`, `getgrnam`, `setpwent`, `endpwent`, etc. via `[LibraryImport]`.
- **Type alignment (Rule 9)** — `LocalUser` and `LocalGroup` inherit `LocalPrincipal`. `SID` is `SecurityIdentifier?`. `PrincipalSource` is an enum.
- **Elevation** — Subprocess errors translated to `"CmdletName requires root privileges."` (Error ID: `UnauthorizedAccess`).
- **Locking** — Per-call locks on shared libc state (`AccountHelpers` pattern).

---

## C# Conventions

| Rule | Detail |
|---|---|
| **Target** | `net8.0`, `TreatWarningsAsErrors=true`, `Deterministic=true` |
| **SMA** | Pinned to `7.4.6` exactly |
| **Namespaces** | File-scoped (`namespace Foo;`) |
| **P/Invoke** | `[LibraryImport("libc")]` + `partial` methods, `AllowUnsafeBlocks=true` |
| **errno** | `Marshal.GetLastWin32Error()` called immediately after P/Invoke |
| **Process** | `ProcessStartInfo.ArgumentList` only, `ReadToEndAsync()` on stdout/stderr |
| **Cmdlets** | `SupportsShouldProcess` on write cmdlets only |
| **Async** | `ConfigureAwait(false)` on all async methods |
| **Errors** | `ErrorRecord` with `UnauthorizedAccess` ID, `SecurityError` category |
| **Copyright** | `// Copyright (c) peppekerstens. All rights reserved.` |

Full rules: `docs/linux-rules.md`

### Version alignment
- **Single source of truth:** `<Version>` in `.csproj`
- **Must match:** `STATUS.md` `**Version:**` line, README.md version history table (latest entry)
- **Bump rule:** `.csproj` first, then `STATUS.md`, then README.md — in that order

---

## Testing

- **Framework:** Pester 5
- **Runner:** `pwsh -c "Invoke-Pester ./tests/"`
- **GHA:** 5-distro matrix + Windows
- **Test file:** `tests/LocalAccounts.Linux.Native.Tests.ps1`

---

## Current State

See `STATUS.md` for module state, open issues, and next steps.

**Open issues:** 0 — fully compliant with all rules.

---

## Boundaries

### What lives in this repo
- Source code (`src/LocalAccounts.Linux.Native/`)
- Pester tests (`tests/`)
- CI/CD (`.github/workflows/`)
- Module status (`STATUS.md`)
- Contributor guide (`AGENTS.md`)
- Development rules (`docs/linux-rules.md`)
- OpenCode config (`.opencode/`)

### What lives elsewhere
- Cross-repo planning, status aggregation, project plan → https://github.com/peppekerstens/opencode
- Other modules → https://github.com/peppekerstens/
- Blog posts → https://peppekerstens.github.io

### What to do when
| Scenario | Where |
|---|---|
| Bug in this module | File issue in **this repo** |
| Feature request for this module | File issue in **this repo** |
| Cross-module convention change | File issue in **opencode** |

---

## Coordination

This module is part of a larger project. Cross-repo planning lives at:
- **Coordination repo:** https://github.com/peppekerstens/opencode
- **Project plan:** https://github.com/peppekerstens/opencode/blob/main/plan.md
