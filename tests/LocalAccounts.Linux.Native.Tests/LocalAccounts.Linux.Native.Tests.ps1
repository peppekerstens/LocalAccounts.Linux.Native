#Requires -Modules @{ ModuleName = 'Pester'; ModuleVersion = '5.2.0' }

BeforeDiscovery {
    $script:onLinux = $IsLinux -eq $true

    $script:allCmdlets = @(
        'Get-LocalUser','New-LocalUser','Set-LocalUser','Enable-LocalUser','Disable-LocalUser',
        'Remove-LocalUser','Rename-LocalUser',
        'Get-LocalGroup','New-LocalGroup','Set-LocalGroup','Remove-LocalGroup','Rename-LocalGroup',
        'Get-LocalGroupMember','Add-LocalGroupMember','Remove-LocalGroupMember'
    )

    $script:readCmdlets  = @('Get-LocalUser','Get-LocalGroup','Get-LocalGroupMember')
    $script:writeCmdlets = $script:allCmdlets | Where-Object { $_ -notin $script:readCmdlets }
}

Describe 'Module: LocalAccounts.Linux.Native' {

    BeforeAll {
        # Locate the built DLL relative to this test file.
        # Supports both local (bin/Debug) and CI (bin/Release) layouts.
        $script:testRoot = if ($PSScriptRoot) { $PSScriptRoot } else { Split-Path $PSCommandPath -Parent }
        $script:repoRoot = Split-Path (Split-Path $script:testRoot -Parent) -Parent

        $script:dllDebug   = Join-Path $script:repoRoot 'src/LocalAccounts.Linux.Native/bin/Debug/net8.0/LocalAccounts.Linux.Native.dll'
        $script:dllRelease = Join-Path $script:repoRoot 'src/LocalAccounts.Linux.Native/bin/Release/net8.0/LocalAccounts.Linux.Native.dll'

        $script:dllPath = if (Test-Path $script:dllRelease) { $script:dllRelease }
                          elseif (Test-Path $script:dllDebug) { $script:dllDebug }
                          else { $null }

        if ($IsLinux -and $script:dllPath) {
            Import-Module $script:dllPath -Force -ErrorAction Stop
        }
    }

    AfterAll {
        if ($IsLinux) {
            Remove-Module 'LocalAccounts.Linux.Native' -Force -ErrorAction SilentlyContinue
        }
    }

    Context 'DLL exists' {
        It 'built DLL is present (Debug or Release)' -Skip:(-not $script:onLinux) {
            $script:dllPath | Should -Not -BeNullOrEmpty
            $script:dllPath | Should -Exist
        }
    }

    Context 'Module surface' -Skip:(-not $script:onLinux) {
        It 'exports cmdlet <_>' -ForEach $script:allCmdlets {
            Get-Command -Module 'LocalAccounts.Linux.Native' -Name $_ | Should -Not -BeNullOrEmpty
        }
        It 'exports exactly 15 cmdlets' {
            $count = (Get-Command -Module 'LocalAccounts.Linux.Native' | Measure-Object).Count
            $count | Should -Be 15
        }
    }

    Context 'Get-LocalUser' -Skip:(-not $script:onLinux) {
        It 'returns at least one user' {
            $users = Get-LocalUser
            $users | Should -Not -BeNullOrEmpty
        }
        It 'returns LocalUser objects with Name property' {
            $users = Get-LocalUser
            $users[0].Name | Should -Not -BeNullOrEmpty
        }
        It 'returns objects with Enabled (bool) property' {
            $users = Get-LocalUser
            $users[0].Enabled | Should -BeOfType [bool]
        }
        It 'returns objects with PasswordRequired (bool) property' {
            $users = Get-LocalUser
            $users[0].PasswordRequired | Should -BeOfType [bool]
        }
        It 'returns objects with UID (int) property' {
            $users = Get-LocalUser
            $users[0].UID | Should -BeOfType [int]
        }
        It 'root user exists' {
            $root = Get-LocalUser -Name root
            $root | Should -Not -BeNullOrEmpty
            $root.Name | Should -Be 'root'
        }
        It 'wildcard filter works' {
            $users = Get-LocalUser -Name 'r*'
            $users | Should -Not -BeNullOrEmpty
            $users | ForEach-Object { $_.Name | Should -BeLike 'r*' }
        }
        It 'returns nothing for nonexistent user' {
            $result = Get-LocalUser -Name 'thereisnosuchuser_xyzzy'
            $result | Should -BeNullOrEmpty
        }
        It 'output type is Microsoft.PowerShell.Commands.LocalUser' {
            $user = Get-LocalUser -Name root
            $user.GetType().FullName | Should -Be 'Microsoft.PowerShell.Commands.LocalUser'
        }
    }

    Context 'Get-LocalGroup' -Skip:(-not $script:onLinux) {
        It 'returns at least one group' {
            Get-LocalGroup | Should -Not -BeNullOrEmpty
        }
        It 'returns objects with Name property' {
            (Get-LocalGroup)[0].Name | Should -Not -BeNullOrEmpty
        }
        It 'returns objects with GID (int) property' {
            (Get-LocalGroup)[0].GID | Should -BeOfType [int]
        }
        It 'root group exists' {
            $root = Get-LocalGroup -Name root
            $root | Should -Not -BeNullOrEmpty
            $root.Name | Should -Be 'root'
        }
        It 'wildcard filter works' {
            $groups = Get-LocalGroup -Name 'r*'
            $groups | Should -Not -BeNullOrEmpty
            $groups | ForEach-Object { $_.Name | Should -BeLike 'r*' }
        }
        It 'returns nothing for nonexistent group' {
            Get-LocalGroup -Name 'thereisnosuchgroup_xyzzy' | Should -BeNullOrEmpty
        }
        It 'output type is Microsoft.PowerShell.Commands.LocalGroup' {
            (Get-LocalGroup -Name root).GetType().FullName | Should -Be 'Microsoft.PowerShell.Commands.LocalGroup'
        }
    }

    Context 'Get-LocalGroupMember' -Skip:(-not $script:onLinux) {
        It 'returns members of root group' {
            Get-LocalGroupMember -Group root | Should -Not -BeNullOrEmpty
        }
        It 'returned members have Name property' {
            (Get-LocalGroupMember -Group root)[0].Name | Should -Not -BeNullOrEmpty
        }
        It 'returned members have ObjectClass = User' {
            (Get-LocalGroupMember -Group root)[0].ObjectClass | Should -Be 'User'
        }
        It 'errors on nonexistent group' {
            { Get-LocalGroupMember -Group 'thereisnosuchgroup_xyzzy' -ErrorAction Stop } | Should -Throw
        }
        It 'output type is Microsoft.PowerShell.Commands.LocalPrincipal' {
            (Get-LocalGroupMember -Group root)[0].GetType().FullName |
                Should -Be 'Microsoft.PowerShell.Commands.LocalPrincipal'
        }
    }

    Context 'Write cmdlets support -WhatIf' -Skip:(-not $script:onLinux) {
        It '<_> has WhatIf parameter' -ForEach $script:writeCmdlets {
            $cmd = Get-Command -Module 'LocalAccounts.Linux.Native' -Name $_
            $cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
        }
    }

    Context 'WhatIf safety' -Skip:(-not $script:onLinux) {
        It 'New-LocalUser -WhatIf does not throw' {
            { New-LocalUser -Name 'testuser_whatif_xyzzy' -WhatIf } | Should -Not -Throw
        }
        It 'New-LocalGroup -WhatIf does not throw' {
            { New-LocalGroup -Name 'testgroup_whatif_xyzzy' -WhatIf } | Should -Not -Throw
        }
        It 'Remove-LocalUser -WhatIf does not throw (nonexistent user ok)' {
            { Remove-LocalUser -Name 'testuser_whatif_xyzzy' -WhatIf } | Should -Not -Throw
        }
    }
}
