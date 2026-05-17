---
description: Build and test LocalAccounts.Linux.Native
agent: build
---
Build and test this module.

**Step 1 — Build**
```bash
dotnet build -c Release
```
Report: warnings count, errors count. Fail if any errors or warnings.

**Step 2 — Test**
```bash
pwsh -NoProfile -NonInteractive -Command "
  Import-Module ./src/LocalAccounts.Linux.Native/bin/Release/net8.0/LocalAccounts.Linux.Native.dll
  Invoke-Pester ./tests/ -Output Detailed
"
```
Report: total tests, passed, failed, skipped. List any failures with error messages.

**Step 3 — Summary**
Report overall status: ✅ all green / ⚠️ warnings / ❌ failures.
If failures, identify root cause and propose a fix.
