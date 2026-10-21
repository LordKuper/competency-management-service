---
name: role-set-hr-assignment-facts
description: Account roles as a set (is_global_admin/is_hr, one role claim each), HR assigned by employee, CanBeHr copies, desk command count, tests that compile but break at run time
metadata:
  type: project
---

- Roles are two bool columns on `users`; API `roles: UserRole[]` built by the static `AppUser.RolesOf(isGlobalAdmin, isHr)`, which EF evaluates client-side in a final `Select` (an instance property of the entity would not translate). Journal role string: `PlatformClaims.JournalRole` (`GlobalAdmin,Hr`, `User` when none); access checks only via `ICurrentActor.IsInRole` / `IsGlobalAdmin()` / `IsHr()` in Platform.
- `org_unit_hr.employee_id` holds the assignment; it acts through the account bound now, `UserStatus.CanBeHr`. The rule exists three times on purpose: `UserStatus.CanBeHr`, `UserDirectory.HasHrCandidateAsync` (LINQ) and the keyless `EmployeeAccount.can_be_hr` SQL in OrgStructure. Change one, change all three.
- `CatalogStore.ViewerAsync` skips route entries for HR on purpose: draft levels only belong to draft tracks, which no matrix uses, and HR reads every no-unit draft. Do not "fix" the early return.
- Career desk source: own account by id, then one `FindByEmployeeIdsAsync` for assigned HR and heads, skipped when neither admin nor involved, so the command count stays as before for plain users; an admin with assignments pays one extra command.
- Contract changes here never break `dotnet build` of `tests/Competency.Tests` (tests go through JSON and SQL strings); they break at run time, and `tests/perf/seed-10k.sql` holds raw `users`/`org_unit_hr` columns. Web `typecheck` (part of `lint` and `build`) does break.

**Why:** each of these is invisible from one file and cost a reasoning pass or would mislead a later fix.
**How to apply:** any change to account roles, HR assignment, desk source reads or the users schema. See [[contract-reset-and-squash-facts]].
