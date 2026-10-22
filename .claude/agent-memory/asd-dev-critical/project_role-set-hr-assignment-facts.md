---
name: role-set-hr-assignment-facts
description: Account roles as Identity roles (roles/user_roles, one role claim each), HR assigned by employee, CanBeHr copies, desk command count, tests that compile but break at run time
metadata:
  type: project
---

- Roles are Identity roles (`roles`/`user_roles`, fixed ids in `UserRoleIds`); read by `AccountRoles.RolesAsync`, changed only by `AccountRoles.SetRolesAsync` (diff only: Identity errors on a repeated add or a missing remove; each Identity call saves the user, so `Version` moves 1-2 extra). `AppUser.UserRoles` is never loaded: LINQ uses `UserRoles.Any(r => r.RoleId == id)`; `UserResponse.Projection` passes `UserRoles.Select(r => r.RoleId).ToList()` to a static mapper, which EF translates as a LEFT JOIN collection (checked with `ToQueryString`), while the in-memory `From` takes roles explicitly. Journal role string: `PlatformClaims.JournalRole` (`GlobalAdmin,Hr`, `User` when none); access checks only via `ICurrentActor.IsInRole` / `IsGlobalAdmin()` / `IsHr()` in Platform.
- `ToQueryString` without a database: a file-based probe `Competency.Tests.cs` (the name matches `InternalsVisibleTo`) with `#:project src/Competency.Api/Competency.Api.csproj`, `#:property PublishAot=false`, run with `dotnet run <file> -c Release`, building the context from `new Competency.Api.AppDbContextFactory().CreateDbContext([])`.
- `org_unit_hr.employee_id` holds the assignment; it acts through the account bound now, `UserStatus.CanBeHr`. The rule exists three times on purpose: `UserStatus.CanBeHr`, `UserDirectory.HasHrCandidateAsync` (LINQ) and the keyless `EmployeeAccount.can_be_hr` SQL in OrgStructure. Change one, change all three.
- `CatalogStore.ViewerAsync` skips the route walk for administrators only. HR needs it: a matrix may use a draft track of its unit, and an HR who is the superior approver but not that unit's responsible HR reads the track and its draft levels only through `RouteEntryIds`.
- Career desk source: own account by id, then one `FindByEmployeeIdsAsync` for assigned HR and heads, skipped when neither admin nor involved, so the command count stays as before for plain users; an admin with assignments pays one extra command.
- Contract changes here never break `dotnet build` of `tests/Competency.Tests` (tests go through JSON and SQL strings); they break at run time, and `tests/perf/seed-10k.sql` and `tests/Competency.Tests/Infrastructure/RowLock.cs` hold raw `users`/`org_unit_hr` SQL. Web `typecheck` (part of `lint` and `build`) does break.

**Why:** each of these is invisible from one file and cost a reasoning pass or would mislead a later fix.
**How to apply:** any change to account roles, HR assignment, desk source reads or the users schema. See [[contract-reset-and-squash-facts]].
