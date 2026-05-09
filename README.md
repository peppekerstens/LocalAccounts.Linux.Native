# LocalAccounts.Linux.Native

A native C# PowerShell binary module implementing the `*-LocalUser`, `*-LocalGroup`, and `*-LocalGroupMember` cmdlets for Linux, as a direct port of the [`PowerShell.LocalAccounts.Linux`](https://github.com/peppekerstens/PowerShell.LocalAccounts.Linux) PowerShell script module (Stage 1/Stage 3).

This is Stage 5 (Tier 2) of the [PowerShell Linux Commands](https://peppekerstens.github.io) project.

## Cmdlets

| Cmdlet | Tool | Notes |
|---|---|---|
| `Get-LocalUser` | `getent passwd`, `passwd -S`, `chage -l` | Wildcard-filtered; matches Windows output shape |
| `New-LocalUser` | `useradd`, `chpasswd`, `passwd -d`, `usermod -L` | |
| `Set-LocalUser` | `usermod`, `chpasswd`, `chage` | |
| `Remove-LocalUser` | `userdel` | `-RemoveHome` flag |
| `Enable-LocalUser` | `usermod -U` | |
| `Disable-LocalUser` | `usermod -L` | |
| `Rename-LocalUser` | `usermod -l` | Optional `-MoveHome` |
| `Get-LocalGroup` | `getent group` | Wildcard-filtered |
| `New-LocalGroup` | `groupadd` | |
| `Set-LocalGroup` | validates via `getent group` | Description field no-op on Linux |
| `Remove-LocalGroup` | `groupdel` | |
| `Rename-LocalGroup` | `groupmod -n` | |
| `Get-LocalGroupMember` | `getent group`, `getent passwd` | Includes primary group members |
| `Add-LocalGroupMember` | `usermod -aG` | |
| `Remove-LocalGroupMember` | `gpasswd -d` | |

## Build

```powershell
dotnet build src/LocalAccounts.Linux.Native/LocalAccounts.Linux.Native.csproj
```

## Test (WSL2 / Linux)

```powershell
Invoke-Pester -Path tests/LocalAccounts.Linux.Native.Tests/ -Output Detailed
```

## Requirements

- Linux only (uses `getent`, `useradd`, `usermod`, `userdel`, `groupadd`, `groupmod`, `groupdel`, `gpasswd`, `chpasswd`, `passwd`, `chage`)
- Most write operations require root or sudo
- PowerShell 7.4+, .NET 8

## Output type compatibility

Output objects (`LocalUser`, `LocalGroup`, `LocalPrincipal`) are defined as real C# classes in the `Microsoft.PowerShell.Commands` namespace, matching the Windows `Microsoft.PowerShell.LocalAccounts` module's type names for script compatibility.
