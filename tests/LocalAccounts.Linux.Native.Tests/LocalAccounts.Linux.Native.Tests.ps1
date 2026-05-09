#Requires -Modules @{ ModuleName = 'Pester'; ModuleVersion = '5.2.0' }

BeforeDiscovery {
    $script:onLinux = $IsLinux -eq $true
    $script:isRoot  = $IsLinux -and (
                          [System.IO.File]::ReadAllText('/proc/self/status') -match '(?m)^Uid:\s+(\d+)' -and
                          $Matches[1] -eq '0')

    $script:allCmdlets = @(
        'Get-LocalUser','New-LocalUser','Set-LocalUser','Enable-LocalUser','Disable-LocalUser',
        'Remove-LocalUser','Rename-LocalUser',
        'Get-LocalGroup','New-LocalGroup','Set-LocalGroup','Remove-LocalGroup','Rename-LocalGroup',
        'Get-LocalGroupMember','Add-LocalGroupMember','Remove-LocalGroupMember'
    )

    $script:readCmdlets  = @('Get-LocalUser','Get-LocalGroup','Get-LocalGroupMember')
    $script:writeCmdlets = $script:allCmdlets | Where-Object { $_ -notin $script:readCmdlets }

    $script:prefix    = 'pla_test_'
    $script:userNames = 1..10 | ForEach-Object { "${script:prefix}u$_" }
    $script:grpNames  = @("${script:prefix}grpA", "${script:prefix}grpB")
}

Describe 'Module: LocalAccounts.Linux.Native' {

    BeforeAll {
        $script:prefix    = 'pla_test_'
        $script:userNames = 1..10 | ForEach-Object { "${script:prefix}u$_" }
        $script:grpNames  = @("${script:prefix}grpA", "${script:prefix}grpB")

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

    # ------------------------------------------------------------------ #
    #  Module surface                                                      #
    # ------------------------------------------------------------------ #

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
            (Get-Command -Module 'LocalAccounts.Linux.Native' | Measure-Object).Count | Should -Be 15
        }
    }

    # ------------------------------------------------------------------ #
    #  Read-only: Get-LocalUser                                           #
    # ------------------------------------------------------------------ #

    Context 'Get-LocalUser' -Skip:(-not $script:onLinux) {
        It 'returns at least one user' {
            Get-LocalUser | Should -Not -BeNullOrEmpty
        }
        It 'Name property is populated' {
            (Get-LocalUser)[0].Name | Should -Not -BeNullOrEmpty
        }
        It 'Enabled property is bool' {
            (Get-LocalUser)[0].Enabled | Should -BeOfType [bool]
        }
        It 'PasswordRequired property is bool' {
            (Get-LocalUser)[0].PasswordRequired | Should -BeOfType [bool]
        }
        It 'UID property is int' {
            (Get-LocalUser)[0].UID | Should -BeOfType [int]
        }
        It 'root user exists' {
            $root = Get-LocalUser -Name root
            $root | Should -Not -BeNullOrEmpty
            $root.Name | Should -Be 'root'
        }
        It 'root user UID is 0' {
            (Get-LocalUser -Name root).UID | Should -Be 0
        }
        It 'wildcard filter works' {
            $users = Get-LocalUser -Name 'r*'
            $users | Should -Not -BeNullOrEmpty
            $users | ForEach-Object { $_.Name | Should -BeLike 'r*' }
        }
        It 'exact name miss returns nothing' {
            Get-LocalUser -Name 'thereisnosuchuser_xyzzy' | Should -BeNullOrEmpty
        }
        It 'output type is Microsoft.PowerShell.Commands.LocalUser' {
            (Get-LocalUser -Name root).GetType().FullName |
                Should -Be 'Microsoft.PowerShell.Commands.LocalUser'
        }
        It 'shell property is a non-empty string for root' {
            (Get-LocalUser -Name root).Shell | Should -Not -BeNullOrEmpty
        }
        It 'home directory property is set for root' {
            (Get-LocalUser -Name root).HomeDirectory | Should -Not -BeNullOrEmpty
        }
        It 'pipeline: Get-LocalUser | Where-Object works' {
            # just verify the pipeline does not throw; result count varies per system
            { Get-LocalUser | Where-Object { -not $_.Enabled } | Out-Null } | Should -Not -Throw
        }
    }

    # ------------------------------------------------------------------ #
    #  Read-only: Get-LocalGroup                                          #
    # ------------------------------------------------------------------ #

    Context 'Get-LocalGroup' -Skip:(-not $script:onLinux) {
        It 'returns at least one group' {
            Get-LocalGroup | Should -Not -BeNullOrEmpty
        }
        It 'Name property is populated' {
            (Get-LocalGroup)[0].Name | Should -Not -BeNullOrEmpty
        }
        It 'GID property is int' {
            (Get-LocalGroup)[0].GID | Should -BeOfType [int]
        }
        It 'root group exists' {
            $g = Get-LocalGroup -Name root
            $g | Should -Not -BeNullOrEmpty
            $g.Name | Should -Be 'root'
        }
        It 'root group GID is 0' {
            (Get-LocalGroup -Name root).GID | Should -Be 0
        }
        It 'wildcard filter works' {
            $groups = Get-LocalGroup -Name 'r*'
            $groups | Should -Not -BeNullOrEmpty
            $groups | ForEach-Object { $_.Name | Should -BeLike 'r*' }
        }
        It 'exact name miss returns nothing' {
            Get-LocalGroup -Name 'thereisnosuchgroup_xyzzy' | Should -BeNullOrEmpty
        }
        It 'output type is Microsoft.PowerShell.Commands.LocalGroup' {
            (Get-LocalGroup -Name root).GetType().FullName |
                Should -Be 'Microsoft.PowerShell.Commands.LocalGroup'
        }
    }

    # ------------------------------------------------------------------ #
    #  Read-only: Get-LocalGroupMember                                    #
    # ------------------------------------------------------------------ #

    Context 'Get-LocalGroupMember' -Skip:(-not $script:onLinux) {
        It 'returns members of root group' {
            Get-LocalGroupMember -Group root | Should -Not -BeNullOrEmpty
        }
        It 'members have Name property' {
            (Get-LocalGroupMember -Group root)[0].Name | Should -Not -BeNullOrEmpty
        }
        It 'members have ObjectClass = User' {
            (Get-LocalGroupMember -Group root)[0].ObjectClass | Should -Be 'User'
        }
        It 'errors on nonexistent group' {
            { Get-LocalGroupMember -Group 'thereisnosuchgroup_xyzzy' -ErrorAction Stop } | Should -Throw
        }
        It 'output type is Microsoft.PowerShell.Commands.LocalPrincipal' {
            (Get-LocalGroupMember -Group root)[0].GetType().FullName |
                Should -Be 'Microsoft.PowerShell.Commands.LocalPrincipal'
        }
        It 'root is a member of root group (primary GID inclusion)' {
            $members = Get-LocalGroupMember -Group root
            $members.Name | Should -Contain 'root'
        }
    }

    # ------------------------------------------------------------------ #
    #  WhatIf plumbing                                                    #
    # ------------------------------------------------------------------ #

    Context 'Write cmdlets support -WhatIf' -Skip:(-not $script:onLinux) {
        It '<_> has WhatIf parameter' -ForEach $script:writeCmdlets {
            (Get-Command -Module 'LocalAccounts.Linux.Native' -Name $_).Parameters.ContainsKey('WhatIf') |
                Should -BeTrue
        }
    }

    Context 'WhatIf safety' -Skip:(-not $script:onLinux) {
        It 'New-LocalUser -WhatIf does not throw' {
            { New-LocalUser -Name "${script:prefix}whatif" -WhatIf } | Should -Not -Throw
        }
        It 'New-LocalGroup -WhatIf does not throw' {
            { New-LocalGroup -Name "${script:prefix}whatif" -WhatIf } | Should -Not -Throw
        }
        It 'Remove-LocalUser -WhatIf does not throw for nonexistent user' {
            { Remove-LocalUser -Name "${script:prefix}whatif" -WhatIf } | Should -Not -Throw
        }
        It 'Set-LocalUser -WhatIf does not throw' {
            { Set-LocalUser -Name 'root' -Description 'ignored' -WhatIf } | Should -Not -Throw
        }
        It 'Add-LocalGroupMember -WhatIf does not throw' {
            { Add-LocalGroupMember -Group 'root' -Member 'root' -WhatIf } | Should -Not -Throw
        }
    }

    # ------------------------------------------------------------------ #
    #  Integration: 10 users                                              #
    # ------------------------------------------------------------------ #

    Context 'Integration - 10 users' -Skip:(-not $script:isRoot) {

        BeforeAll {
            $script:createdUsers = [System.Collections.Generic.List[string]]::new()
            foreach ($name in $script:userNames) {
                New-LocalUser -Name $name -FullName "Test User $name" -NoPassword -Confirm:$false
                if (Get-LocalUser -Name $name -ErrorAction SilentlyContinue) {
                    $script:createdUsers.Add($name)
                }
            }
        }

        AfterAll {
            foreach ($name in ($script:createdUsers | Select-Object)) {
                Remove-LocalUser -Name $name -RemoveHome -Confirm:$false -ErrorAction SilentlyContinue
            }
            Remove-LocalUser -Name "${script:prefix}u1_renamed" -RemoveHome -Confirm:$false -ErrorAction SilentlyContinue
        }

        It 'creates all 10 users' {
            $script:createdUsers.Count | Should -Be 10
        }

        It 'each created user is returned by Get-LocalUser' `
            -ForEach ($script:userNames | ForEach-Object { @{ UserName = $_ } }) {
            Get-LocalUser -Name $UserName | Should -Not -BeNullOrEmpty
        }

        It 'created users have correct FullName' `
            -ForEach ($script:userNames | ForEach-Object { @{ UserName = $_ } }) {
            (Get-LocalUser -Name $UserName).FullName | Should -Be "Test User $UserName"
        }

        It 'created users are enabled' `
            -ForEach ($script:userNames | ForEach-Object { @{ UserName = $_ } }) {
            (Get-LocalUser -Name $UserName).Enabled | Should -BeTrue
        }

        It 'wildcard returns all 10 test users' {
            $found = Get-LocalUser -Name "${script:prefix}u*"
            $found.Count | Should -Be 10
        }

        It 'Set-LocalUser changes FullName' {
            Set-LocalUser -Name "${script:prefix}u3" -FullName 'Updated Name'
            (Get-LocalUser -Name "${script:prefix}u3").FullName | Should -Be 'Updated Name'
        }

        It 'Set-LocalUser changes Shell' {
            Set-LocalUser -Name "${script:prefix}u4" -Shell '/bin/sh'
            (Get-LocalUser -Name "${script:prefix}u4").Shell | Should -Be '/bin/sh'
        }

        It 'Set-LocalUser changes HomeDirectory' {
            Set-LocalUser -Name "${script:prefix}u8" -HomeDirectory "/tmp/${script:prefix}u8_home"
            (Get-LocalUser -Name "${script:prefix}u8").HomeDirectory | Should -Be "/tmp/${script:prefix}u8_home"
        }

        It 'disables a user' {
            & bash -c "echo '${script:prefix}u2:TempPwd123!' | chpasswd" | Out-Null
            Disable-LocalUser -Name "${script:prefix}u2" -Confirm:$false
            (Get-LocalUser -Name "${script:prefix}u2").Enabled | Should -BeFalse
        }

        It 'enables a disabled user' {
            Enable-LocalUser -Name "${script:prefix}u2" -Confirm:$false
            (Get-LocalUser -Name "${script:prefix}u2").Enabled | Should -BeTrue
        }

        It 'Rename-LocalUser renames user 1' {
            Rename-LocalUser -Name "${script:prefix}u1" -NewName "${script:prefix}u1_renamed"
            Get-LocalUser -Name "${script:prefix}u1_renamed" | Should -Not -BeNullOrEmpty
            Get-LocalUser -Name "${script:prefix}u1"         | Should -BeNullOrEmpty
            $script:createdUsers.Remove("${script:prefix}u1") | Out-Null
            $script:createdUsers.Add("${script:prefix}u1_renamed")
        }

        It 'old name is gone after rename' {
            Get-LocalUser -Name "${script:prefix}u1" | Should -BeNullOrEmpty
        }

        It 'removes 3 users (u5, u6, u7)' {
            foreach ($n in @("${script:prefix}u5","${script:prefix}u6","${script:prefix}u7")) {
                Remove-LocalUser -Name $n -RemoveHome -Confirm:$false
                $script:createdUsers.Remove($n) | Out-Null
            }
            Get-LocalUser -Name "${script:prefix}u5" | Should -BeNullOrEmpty
            Get-LocalUser -Name "${script:prefix}u6" | Should -BeNullOrEmpty
            Get-LocalUser -Name "${script:prefix}u7" | Should -BeNullOrEmpty
        }

        It 'remaining tracked users still exist after 3 deletions' {
            $missing = $script:createdUsers | Where-Object { -not (Get-LocalUser -Name $_ -ErrorAction SilentlyContinue) }
            $missing | Should -BeNullOrEmpty
        }

        It 'pipeline: Get-LocalUser wildcard | Remove-LocalUser -WhatIf does not throw' {
            { Get-LocalUser -Name "${script:prefix}u*" | ForEach-Object { Remove-LocalUser -Name $_.Name -WhatIf } } |
                Should -Not -Throw
        }
    }

    # ------------------------------------------------------------------ #
    #  Integration: 2 groups                                              #
    # ------------------------------------------------------------------ #

    Context 'Integration - 2 groups' -Skip:(-not $script:isRoot) {

        BeforeAll {
            $script:createdGroups = [System.Collections.Generic.List[string]]::new()
            foreach ($name in $script:grpNames) {
                New-LocalGroup -Name $name -Confirm:$false
                if (Get-LocalGroup -Name $name -ErrorAction SilentlyContinue) {
                    $script:createdGroups.Add($name)
                }
            }
            $script:grpUsers = @("${script:prefix}gmu1","${script:prefix}gmu2")
            foreach ($u in $script:grpUsers) {
                New-LocalUser -Name $u -NoPassword -Confirm:$false
            }
        }

        AfterAll {
            foreach ($name in ($script:createdGroups | Select-Object)) {
                Remove-LocalGroup -Name $name -Confirm:$false -ErrorAction SilentlyContinue
            }
            Remove-LocalGroup -Name "${script:prefix}grpA_renamed" -Confirm:$false -ErrorAction SilentlyContinue
            foreach ($u in $script:grpUsers) {
                Remove-LocalUser -Name $u -RemoveHome -Confirm:$false -ErrorAction SilentlyContinue
            }
        }

        It 'creates both groups' {
            $script:createdGroups.Count | Should -Be 2
        }

        It 'each created group is returned by Get-LocalGroup' `
            -ForEach ($script:grpNames | ForEach-Object { @{ GrpName = $_ } }) {
            Get-LocalGroup -Name $GrpName | Should -Not -BeNullOrEmpty
        }

        It 'created groups have GID > 0' `
            -ForEach ($script:grpNames | ForEach-Object { @{ GrpName = $_ } }) {
            (Get-LocalGroup -Name $GrpName).GID | Should -BeGreaterThan 0
        }

        It 'renames grpA' {
            Rename-LocalGroup -Name "${script:prefix}grpA" -NewName "${script:prefix}grpA_renamed"
            Get-LocalGroup -Name "${script:prefix}grpA_renamed" | Should -Not -BeNullOrEmpty
            Get-LocalGroup -Name "${script:prefix}grpA"         | Should -BeNullOrEmpty
            $script:createdGroups.Remove("${script:prefix}grpA") | Out-Null
            $script:createdGroups.Add("${script:prefix}grpA_renamed")
        }

        It 'old grpA name is gone after rename' {
            Get-LocalGroup -Name "${script:prefix}grpA" | Should -BeNullOrEmpty
        }

        It 'adds both temp users to grpB' {
            Add-LocalGroupMember -Group "${script:prefix}grpB" -Member $script:grpUsers
            $members = (Get-LocalGroupMember -Group "${script:prefix}grpB").Name
            $members | Should -Contain "${script:prefix}gmu1"
            $members | Should -Contain "${script:prefix}gmu2"
        }

        It 'member count of grpB is 2' {
            (Get-LocalGroupMember -Group "${script:prefix}grpB" | Measure-Object).Count |
                Should -Be 2
        }

        It 'removes one member from grpB' {
            Remove-LocalGroupMember -Group "${script:prefix}grpB" -Member "${script:prefix}gmu1" -Confirm:$false
            (Get-LocalGroupMember -Group "${script:prefix}grpB").Name |
                Should -Not -Contain "${script:prefix}gmu1"
        }

        It 'grpB still has the remaining member after removal' {
            (Get-LocalGroupMember -Group "${script:prefix}grpB").Name |
                Should -Contain "${script:prefix}gmu2"
        }

        It 'Set-LocalGroup on renamed group does not throw' {
            { Set-LocalGroup -Name "${script:prefix}grpA_renamed" -Description 'ignored' } |
                Should -Not -Throw
        }

        It 'deletes both groups' {
            foreach ($name in @($script:createdGroups.ToArray())) {
                Remove-LocalGroup -Name $name -Confirm:$false
                $script:createdGroups.Remove($name) | Out-Null
            }
            $script:createdGroups.Count | Should -Be 0
        }

        It 'deleted groups no longer returned by Get-LocalGroup' `
            -ForEach ($script:grpNames | ForEach-Object { @{ GrpName = $_ } }) {
            Get-LocalGroup -Name $GrpName             | Should -BeNullOrEmpty
            Get-LocalGroup -Name "${GrpName}_renamed" | Should -BeNullOrEmpty
        }
    }

    # ------------------------------------------------------------------ #
    #  Integration: end-to-end lifecycle                                  #
    # ------------------------------------------------------------------ #

    Context 'Integration - end-to-end lifecycle' -Skip:(-not $script:isRoot) {

        BeforeAll {
            $script:e2eUser  = "${script:prefix}e2e"
            $script:e2eGroup = "${script:prefix}e2egrp"
            New-LocalUser  -Name $script:e2eUser  -FullName 'E2E User' -NoPassword -Confirm:$false
            New-LocalGroup -Name $script:e2eGroup -Confirm:$false
        }

        AfterAll {
            Remove-LocalUser  -Name $script:e2eUser              -RemoveHome -Confirm:$false -ErrorAction SilentlyContinue
            Remove-LocalUser  -Name "${script:e2eUser}_r"        -RemoveHome -Confirm:$false -ErrorAction SilentlyContinue
            Remove-LocalGroup -Name $script:e2eGroup             -Confirm:$false -ErrorAction SilentlyContinue
        }

        It 'user and group exist' {
            Get-LocalUser  -Name $script:e2eUser  | Should -Not -BeNullOrEmpty
            Get-LocalGroup -Name $script:e2eGroup | Should -Not -BeNullOrEmpty
        }

        It 'adds user to group' {
            Add-LocalGroupMember -Group $script:e2eGroup -Member $script:e2eUser
            (Get-LocalGroupMember -Group $script:e2eGroup).Name | Should -Contain $script:e2eUser
        }

        It 'disables user' {
            & bash -c "echo '${script:e2eUser}:TempPwd123!' | chpasswd" | Out-Null
            Disable-LocalUser -Name $script:e2eUser -Confirm:$false
            (Get-LocalUser -Name $script:e2eUser).Enabled | Should -BeFalse
        }

        It 're-enables user' {
            Enable-LocalUser -Name $script:e2eUser -Confirm:$false
            (Get-LocalUser -Name $script:e2eUser).Enabled | Should -BeTrue
        }

        It 'renames user' {
            Rename-LocalUser -Name $script:e2eUser -NewName "${script:e2eUser}_r"
            Get-LocalUser -Name "${script:e2eUser}_r" | Should -Not -BeNullOrEmpty
            Get-LocalUser -Name $script:e2eUser        | Should -BeNullOrEmpty
        }

        It 'group is still queryable after user rename' {
            { Get-LocalGroupMember -Group $script:e2eGroup -ErrorAction Stop } | Should -Not -Throw
        }

        It 'removes renamed user' {
            Remove-LocalUser -Name "${script:e2eUser}_r" -RemoveHome -Confirm:$false
            Get-LocalUser -Name "${script:e2eUser}_r" | Should -BeNullOrEmpty
        }

        It 'removes group' {
            Remove-LocalGroup -Name $script:e2eGroup -Confirm:$false
            Get-LocalGroup -Name $script:e2eGroup | Should -BeNullOrEmpty
        }
    }

    # ------------------------------------------------------------------ #
    #  Real-world scenario: service account provisioning                  #
    #  Models creating a dedicated system account for a daemon (e.g.      #
    #  a monitoring agent) - no login shell, no home dir, locked password #
    # ------------------------------------------------------------------ #

    Context 'Scenario - service account provisioning' -Skip:(-not $script:isRoot) {

        BeforeAll {
            $script:svcUser  = "${script:prefix}svc"
            $script:svcGroup = "${script:prefix}svcgrp"
            # Create a system-style group first
            New-LocalGroup -Name $script:svcGroup -Confirm:$false
        }

        AfterAll {
            Remove-LocalUser  -Name $script:svcUser  -RemoveHome -Confirm:$false -ErrorAction SilentlyContinue
            Remove-LocalGroup -Name $script:svcGroup -Confirm:$false -ErrorAction SilentlyContinue
        }

        It 'creates service account with nologin shell' {
            New-LocalUser -Name $script:svcUser -NoPassword -Shell '/sbin/nologin' -Confirm:$false
            (Get-LocalUser -Name $script:svcUser).Shell | Should -Match 'nologin'
        }

        It 'service account is enabled after creation' {
            (Get-LocalUser -Name $script:svcUser).Enabled | Should -BeTrue
        }

        It 'service account added to service group' {
            Add-LocalGroupMember -Group $script:svcGroup -Member $script:svcUser
            (Get-LocalGroupMember -Group $script:svcGroup).Name | Should -Contain $script:svcUser
        }

        It 'disabling service account locks it' {
            & bash -c "echo '${script:svcUser}:TempSvc123!' | chpasswd" | Out-Null
            Disable-LocalUser -Name $script:svcUser -Confirm:$false
            (Get-LocalUser -Name $script:svcUser).Enabled | Should -BeFalse
        }

        It 'disabled service account still appears in Get-LocalUser' {
            Get-LocalUser -Name $script:svcUser | Should -Not -BeNullOrEmpty
        }

        It 'disabled accounts discoverable via pipeline filter' {
            $disabled = Get-LocalUser | Where-Object { -not $_.Enabled -and $_.Name -like "${script:prefix}*" }
            $disabled.Name | Should -Contain $script:svcUser
        }

        It 'remove service account from group before deletion' {
            Remove-LocalGroupMember -Group $script:svcGroup -Member $script:svcUser -Confirm:$false
            (Get-LocalGroupMember -Group $script:svcGroup).Name |
                Should -Not -Contain $script:svcUser
        }

        It 'deletes service account' {
            Remove-LocalUser -Name $script:svcUser -RemoveHome -Confirm:$false
            Get-LocalUser -Name $script:svcUser | Should -BeNullOrEmpty
        }

        It 'deletes service group' {
            Remove-LocalGroup -Name $script:svcGroup -Confirm:$false
            Get-LocalGroup -Name $script:svcGroup | Should -BeNullOrEmpty
        }
    }

    # ------------------------------------------------------------------ #
    #  Real-world scenario: operator group with multiple members          #
    #  Models a shared sudo/operator group where users are bulk-managed   #
    # ------------------------------------------------------------------ #

    Context 'Scenario - operator group bulk membership' -Skip:(-not $script:isRoot) {

        BeforeAll {
            $script:opGroup   = "${script:prefix}operators"
            $script:opMembers = @("${script:prefix}op1","${script:prefix}op2","${script:prefix}op3")
            New-LocalGroup -Name $script:opGroup -Confirm:$false
            foreach ($u in $script:opMembers) {
                New-LocalUser -Name $u -NoPassword -Confirm:$false
            }
        }

        AfterAll {
            Remove-LocalGroup -Name $script:opGroup -Confirm:$false -ErrorAction SilentlyContinue
            foreach ($u in $script:opMembers) {
                Remove-LocalUser -Name $u -RemoveHome -Confirm:$false -ErrorAction SilentlyContinue
            }
        }

        It 'adds three operators to group in one call' {
            Add-LocalGroupMember -Group $script:opGroup -Member $script:opMembers
            $members = (Get-LocalGroupMember -Group $script:opGroup).Name
            foreach ($u in $script:opMembers) {
                $members | Should -Contain $u
            }
        }

        It 'member count is exactly 3' {
            (Get-LocalGroupMember -Group $script:opGroup | Measure-Object).Count | Should -Be 3
        }

        It 'all operators exist as users' {
            foreach ($u in $script:opMembers) {
                Get-LocalUser -Name $u | Should -Not -BeNullOrEmpty
            }
        }

        It 'bulk disable: pipeline disable all operators' {
            foreach ($u in $script:opMembers) {
                & bash -c "echo '${u}:TempOp123!' | chpasswd" | Out-Null
            }
            $script:opMembers | ForEach-Object { Disable-LocalUser -Name $_ -Confirm:$false }
            $disabled = Get-LocalUser -Name "${script:prefix}op*" | Where-Object { -not $_.Enabled }
            $disabled.Count | Should -Be 3
        }

        It 'bulk enable: re-enable all operators' {
            $script:opMembers | ForEach-Object { Enable-LocalUser -Name $_ -Confirm:$false }
            $enabled = Get-LocalUser -Name "${script:prefix}op*" | Where-Object { $_.Enabled }
            $enabled.Count | Should -Be 3
        }

        It 'remove all members from group via pipeline' {
            $script:opMembers | ForEach-Object {
                Remove-LocalGroupMember -Group $script:opGroup -Member $_ -Confirm:$false
            }
            $members = Get-LocalGroupMember -Group $script:opGroup -ErrorAction SilentlyContinue
            $members | Should -BeNullOrEmpty
        }

        It 'group still exists after all members removed' {
            Get-LocalGroup -Name $script:opGroup | Should -Not -BeNullOrEmpty
        }
    }

    # ------------------------------------------------------------------ #
    #  Real-world scenario: account expiry and metadata                   #
    # ------------------------------------------------------------------ #

    Context 'Scenario - account expiry and metadata' -Skip:(-not $script:isRoot) {

        BeforeAll {
            $script:expUser = "${script:prefix}expiry"
            New-LocalUser -Name $script:expUser -FullName 'Expiry Test User' -NoPassword -Confirm:$false
        }

        AfterAll {
            Remove-LocalUser -Name $script:expUser -RemoveHome -Confirm:$false -ErrorAction SilentlyContinue
        }

        It 'user created successfully' {
            Get-LocalUser -Name $script:expUser | Should -Not -BeNullOrEmpty
        }

        It 'FullName is set correctly' {
            (Get-LocalUser -Name $script:expUser).FullName | Should -Be 'Expiry Test User'
        }

        It 'Set-LocalUser updates description/FullName' {
            Set-LocalUser -Name $script:expUser -FullName 'Updated Expiry User'
            (Get-LocalUser -Name $script:expUser).FullName | Should -Be 'Updated Expiry User'
        }

        It 'Set-LocalUser sets account expiry date' {
            $expDate = (Get-Date).AddDays(30)
            Set-LocalUser -Name $script:expUser -AccountExpires $expDate
            # Verify it does not throw; chage -l readable by root
            $lu = Get-LocalUser -Name $script:expUser
            $lu | Should -Not -BeNullOrEmpty
        }

        It 'Set-LocalUser clears account expiry (never expires)' {
            Set-LocalUser -Name $script:expUser -AccountNeverExpires
            $lu = Get-LocalUser -Name $script:expUser
            $lu | Should -Not -BeNullOrEmpty
        }
    }
}
