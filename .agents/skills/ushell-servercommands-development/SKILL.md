---
name: ushell-servercommands-development
description: Implementation-specific entry skill for UShell.ServerCommands.Contract (.NET executor contract, ASP.NET Core worker hosting exposed via UJMW, TypeScript package ushell-servercommands-contract). Use before changing code, build or docs in this repository. Contains only implementation details. The server-command target state (frontend command type, progress UI, polling) lives in the root skills of ProjectUShell/UShell.Docs, which must always be loaded as well.
---

# UShell.ServerCommands.Contract - implementation details

> **This skill covers implementation details only.** The end-to-end target state of server
> commands, compatibility rules and backlog live in **UShell.Docs** (root level of the U-Shell
> skill set). Always load them first:
>
> - `ushell-architecture` (mandatory root entry)
> - `ushell-server-commands` (target state and status of this capability)
> - `ushell-data-auth-scope` (token and ApplicationScope side channel for the endpoint)
>
> Location: sibling checkout `../UShell.Docs/.agents/skills/<name>/SKILL.md`, otherwise
> https://github.com/ProjectUShell/UShell.Docs/tree/master/.agents/skills
>
> Design knowledge discovered while working here goes into those skills, not into this file.

## Repository layout

- `dotnet/UShell.ServerCommands.Contract.sln`
  - `src/UShell.ServerCommands.Contract` (shared project) - `[Contract]/IServerCommandExecutor`, DTOs and enums; `[Executor]/CommandExecutor` base implementation; target projects for net48, net8.0, net10.0.
  - `src/UShell.ServerCommands.AspNetCore` (shared project) - `AddServerCommandExecutor(...)`: worker-based executor as hosted service, exposed as UJMW controller (default route `commands`); targets net8.0, net10.0.
  - `test/` - tests.
  - NuGet: `UShell.ServerCommands.Contract.nuspec`, `UShell.ServerCommands.AspNetCore.nuspec`.
- `js/` - TypeScript package `ushell-servercommands-contract` (client-side mirror of the contract; currently a placeholder).
- `doc/` - KVU versioning. **Never edit `doc/changelog.md`.**

## Local rules

- Shared projects: when adding `.cs` files, update the matching `.projitems`.
- The executor contract is consumed over UJMW by the shell. Changes are additive; existing method names, out parameters and enum values keep their meaning.
- Keep the .NET contract and the TypeScript mirror aligned.
- Reference backlog IDs (`UB-xx` from `UShell.Docs/doc/backlog.md`) in commit messages and PR descriptions.
