# Contributing to LocalAccounts.Linux.Native

## What this module is

Linux `*-LocalUser` and `*-LocalGroup` cmdlets via P/Invoke libc. 15 cmdlets.
Part of the [PowerShell Linux Commands](https://github.com/peppekerstens/opencode) project.

## Prerequisites

- .NET 8 SDK
- PowerShell 7.4+
- Docker or Podman (for multi-distro tests)

## Quick start

```bash
# Clone
git clone https://github.com/peppekerstens/LocalAccounts.Linux.Native.git
cd LocalAccounts.Linux.Native

# Build
dotnet build -c Release

# Run tests
pwsh -c "Import-Module ./src/LocalAccounts.Linux.Native/bin/Release/net8.0/LocalAccounts.Linux.Native.dll; Invoke-Pester ./tests/"
```

## Your First Contribution

1. Find an issue labeled [`good first issue`](https://github.com/peppekerstens/LocalAccounts.Linux.Native/labels/good%20first%20issue)
2. Comment on the issue: "I'd like to work on this"
3. Fork the repo and create a branch: `fix/issue-42-xml-docs`
4. Make your changes
5. Run `dotnet build -c Release` — must be 0 warnings
6. Run `pwsh -c "Invoke-Pester ./tests/"` — must pass
7. Push and open a PR

The CI will automatically check your build and tests. Once green, a maintainer will review and merge.

## Commit Messages

We use [conventional commits](https://www.conventionalcommits.org/):

```
fix: resolve pipe deadlock in AccountHelpers.Run()
feat: add PrincipalSource enum
docs: update STATUS.md for v1.0.0
test: add elevation error tests for write cmdlets
ci: add pr-validation.yml workflow
refactor: replace string SID with SecurityIdentifier type
```

## Branch Naming

| Prefix | Example |
|---|---|
| `fix/` | `fix/issue-32-elevation-check` |
| `feat/` | `feat/add-clone-method` |
| `docs/` | `docs/update-readme-version` |
| `test/` | `test/add-whatif-tests` |
| `ci/` | `ci/add-pssa-check` |

## Before submitting a PR

- [ ] `dotnet build -c Release` — 0 warnings, 0 errors
- [ ] All Pester tests pass
- [ ] Code follows `docs/linux-rules.md`
- [ ] `STATUS.md` updated if module state changed
- [ ] `CHANGELOG.md` updated with your changes
- [ ] No dead code, unused imports, or silent error swallowing

## Architecture

See `AGENTS.md` for module structure, design decisions, and conventions.

## Reporting issues

- **Bugs:** Use the [Bug Report](https://github.com/peppekerstens/LocalAccounts.Linux.Native/issues/new?template=bug-report.md) template
- **Features:** Use the [Feature Request](https://github.com/peppekerstens/LocalAccounts.Linux.Native/issues/new?template=feature-request.md) template
- **Code review findings:** Use the [Code Review](https://github.com/peppekerstens/LocalAccounts.Linux.Native/issues/new?template=code-review.md) template

## Cross-repo context

This module is part of a larger project. Cross-repo planning lives at:
- **Coordination repo:** https://github.com/peppekerstens/opencode
- **Project plan:** https://github.com/peppekerstens/opencode/blob/main/plan.md
