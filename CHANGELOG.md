# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.0] — 2026-05-17

### Added
- Rule 9 cross-platform type alignment: `LocalUser` and `LocalGroup` now inherit from `LocalPrincipal`
- `SID` property type changed from `string?` to `SecurityIdentifier?`
- `PrincipalSource` property type changed from `string` to `PrincipalSource?` enum
- `ToString()` override on `LocalPrincipal`, `LocalUser`, `LocalGroup`
- `Clone()` methods on `LocalUser` and `LocalGroup`
- `PrincipalSource` enum matching Windows `System.Security.Principal.PrincipalSource`
- `System.Security.Principal.Windows` 5.0.0 NuGet package reference
- Elevation error translation across all write cmdlets
- `STATUS.md` and `AGENTS.md` contributor documentation
- `CONTRIBUTING.md`, `CODE_OF_CONDUCT.md`, `SECURITY.md`
- CODEOWNERS file
- PR validation workflow (`pr-validation.yml`)
- GitHub issue templates (bug report, feature request, code review finding)
- PR template with build/test checklist
- OpenCode configuration (`.opencode/`) for standalone development

### Fixed
- Error ID changed to `UnauthorizedAccess`, category to `SecurityError` (Rule 6)
- Pipe deadlock risks in `AccountHelpers.Run()` and `RunWithStdin()` — now reads stdout/stderr concurrently
- Error ID typo `"UseraaddFailed"` → `"UserAddFailed"`
- Elevation tests on Windows now skipped correctly

### Changed
- Copyright headers updated to `peppekerstens` (Rule 10)
- All 22 linux-rules.md applied and verified

## [0.3.0] — 2026-05-09

### Fixed
- `$script:isLinux` collision → `$script:onLinux`
- `shell: pwsh` in pester.yml
- `--privileged` container flag
- `BeOfType` null quirk → `Should -Not -Throw`
- openSUSE image now includes `gawk`+`findutils`

## [0.2.0] — 2026-05-08

### Added
- Service account scenario tests (nologin shell, lock, remove)
- Operator bulk membership tests (pipeline disable/enable/remove)
- Account expiry tests (`Set-LocalUser -AccountExpires/-AccountNeverExpires`)
- Primary-group edge case tests (`root` in `root` group)
- NUnitXML test output and artifact upload
- Windows pester job

## [0.1.0] — 2026-05-05

### Added
- Initial release
- All 15 cmdlets implemented via P/Invoke libc
- 116 Pester tests
- 5-distro GHA matrix (Ubuntu 24.04, Debian 12, Fedora 40, openSUSE Tumbleweed, Arch Linux)
