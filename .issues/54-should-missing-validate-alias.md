---
name: SHOULD — Name parameter lacks ValidateNotNullOrEmpty and Alias on write cmdlets
labels: [enhancement, SHOULD]
---

## Rule violated
- **Rule number:** Rule 14
- **Rule name:** Parameter attributes must include Position, ValidateNotNullOrEmpty, and Alias

## Location
- **File:** `src/LocalAccounts.Linux.Native/Commands/NewLocalUserCommand.cs`, lines 14-15
- **File:** `src/LocalAccounts.Linux.Native/Commands/NewLocalGroupCommand.cs`
- **File:** Other write cmdlets with Name parameters

## What's wrong
`NewLocalUserCommand.Name` has `Position = 0` but lacks `[ValidateNotNullOrEmpty]` and `[Alias()]`. Rule 14 requires all three.

```csharp
[Parameter(Mandatory = true, Position = 0)]
public string Name { get; set; } = string.Empty;  // ❌ missing ValidateNotNullOrEmpty, Alias
```

## How to fix
```csharp
[Parameter(Mandatory = true, Position = 0)]
[ValidateNotNullOrEmpty]
[Alias("UserName")]
public string Name { get; set; } = string.Empty;
```

## Severity
- [ ] SHOULD — should be fixed before merge
