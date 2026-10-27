---
responsibility:
  owns: project-owner custom rules read by all agents in all phases
  excludes: phase-specific rules (design-only, coding-only)
  delegates_to: custom-design-rules.md (design/design-review), custom-coding-rules.md (impl/impl-review), .asd/rules/ (workflow rules)
---

# Custom Common Rules

Universal project rules read by every ASD agent in every phase. Put here: domain glossary, naming conventions, compliance requirements, project-wide vocabulary, anything holding across design AND code.

Phase-scoped rules go elsewhere — design constraints to `custom-design-rules.md`, code/test constraints to `custom-coding-rules.md`. ASD never overwrites this file.

## Docker safety

- Agents never run destructive Docker commands (`down -v`, `volume rm`, `volume prune`) against the user's dev stack or volumes with a fixed name. Temporary runs use only their own named volumes and delete them by exact name.

## Agent memory edits

- Agents change an existing memory file with a targeted edit and never overwrite a memory file whole. (`002-mail-invite-password-reset#A-2`)

## No financial data

- The system stores no financial information: salaries, salary ranges, budgets. No field, column, endpoint or screen carries it.

## No backward compatibility before 1.0

- Until release 1.0 the project keeps no API backward compatibility: the contract version stays 1.0.0 and is not bumped before release; obsolete surface is deleted, never marked deprecated. The DB schema changes by incremental EF migrations, with data transfer where the working development database would otherwise fail to migrate or lose meaning; right before release the migrations are squashed into one InitialCreate and every database is recreated (`docs/architecture/stack.html` «Контракт из кода»). (`004-role-desktop-user-journeys#AC-58`, `#AC-63`)

## Blocking project processes

- An agent may stop a user-started process of this project that blocks a build, a test run or a file, and only these: Competency.Api (`Competency.Api.exe`, or `dotnet` running this repository's `Competency.Api.dll`); the Visual Studio instance with this repository's `Competency.slnx` open (`devenv.exe`) and its build processes (`MSBuild.exe`, `VBCSCompiler.exe`, `ServiceHub.*`); `testhost.exe` and `dotnet test` of this repository; the frontend dev server (`node` running `vite` from `web/`).
- The process is stopped by PID, found by this repository's path or command line, never by whole image name. The browser, Docker and any other process are never stopped. (`005-org-structure-excel-import#AC-25`, replaces rejected `004-role-desktop-user-journeys#A-4`, `#A-7`)
