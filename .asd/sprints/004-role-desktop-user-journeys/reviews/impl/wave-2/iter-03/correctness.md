[REVIEW-impl-correctness]: APPROVE

# Review — correctness

- **Phase**: impl-review
- **Iteration**: wave-2/iter-03

## Findings

| # | Severity | Location | Description | Suggested fix |
|---|---|---|---|---|

## Coverage (internal reviewers only)

Scope: the 13 manifest files and the iteration diff `df0bd70eb49d498c.diff`; floor high.

- Bugs: `OnboardEmployeePage.tsx:100-112`. The reread is enabled only after a 412 (`isStaleVersion`) and is adopted only when the read version is newer. A stale cached entry cannot be adopted early, because `withLatestEmployee` compares versions. A repeated 412 refetches, because `invalidateOrgStructure` in `onSettled` marks the detail key stale. The render-phase update ends after one pass, because `isRefused` is reset. `onSaved` and `onAnother` reset `isRefused`. `BuilderCompetenciesStep.tsx:434`: explicit `disabled: false` per row used to override `ConfigProvider componentDisabled`, and `isSaving` now closes that gap. `PositionSelect.tsx:144`: `\|\| undefined` stops a false prop from beating the Form disabled context. `EmployeeForm.tsx:132` and `UserForm.tsx:94` use the Form `disabled` context; `useFormSubmit` resets it in `finally`. `TrackPositionsPage.tsx`: the removed `onOpenChange` sat on an uncontrolled search (no `searchValue`), and rc-select clears it on close by itself. `PositionSelect` keeps its own handler because its search is controlled.
- Security / Contracts: there is no trust-boundary, API or schema change in scope.
- AC coverage trace: AC-20 (lock while saving, reread after 412, next save sends the new version) and AC-61 (edits are not replaced by a background refresh) trace to the changes above. The tests in scope pin each path.
- UI conformance: only disabled states change, through the native antd disabled mechanism. No raw values and no new components.
- Below floor, not counted: `OnboardEmployeePage` repeats the reread-after-412 adoption that `useEditBase` already provides (efficiency's domain).

```json
{"manifest_digest":"2df008bf99d2113beeb276757124cd24bd2b81ebe6e7ef467db93233afc6a292","findings":[],"files":[{"i":".claude/agent-memory/asd-tester-critical/MEMORY.md","s":"checked"},{"i":".claude/agent-memory/asd-tester-critical/project_competency-test-harness.md","s":"checked"},{"i":"web/src/features/desk/BuilderCompetenciesStep.test.tsx","s":"checked"},{"i":"web/src/features/desk/BuilderCompetenciesStep.tsx","s":"checked"},{"i":"web/src/features/desk/OnboardEmployeePage.test.tsx","s":"checked"},{"i":"web/src/features/desk/OnboardEmployeePage.tsx","s":"checked"},{"i":"web/src/features/desk/TrackPositionsPage.tsx","s":"checked"},{"i":"web/src/features/org-structure/EmployeeForm.test.tsx","s":"checked"},{"i":"web/src/features/org-structure/EmployeeForm.tsx","s":"checked"},{"i":"web/src/features/org-structure/PositionSelect.test.tsx","s":"checked"},{"i":"web/src/features/org-structure/PositionSelect.tsx","s":"checked"},{"i":"web/src/features/users/UserForm.tsx","s":"checked"},{"i":"web/src/features/users/UserModal.test.tsx","s":"checked"}],"rules":[{"i":"Draft correctness [design-review]","s":"n/a","p":"outside phase gate"},{"i":"Bugs [impl-review]","s":"pass"},{"i":"Security [impl-review]","s":"pass"},{"i":"Contracts [impl-review]","s":"pass"},{"i":"Best practices [impl-review]","s":"pass"},{"i":"AC coverage trace [impl-review]","s":"pass"},{"i":"UI conformance [design-review — `n/a: outside phase gate` without a ux-spec/design-system draft; impl-review — conditional on a UI surface in scope]","s":"pass"},{"i":".asd/project/custom-common-rules.md","s":"pass"},{"i":".asd/project/custom-coding-rules.md","s":"pass"}],"sections":[{"i":"Draft correctness [design-review]","s":"n/a","p":"outside phase gate"},{"i":"Bugs [impl-review]","s":"reviewed"},{"i":"Security [impl-review]","s":"reviewed"},{"i":"Contracts [impl-review]","s":"reviewed"},{"i":"Best practices [impl-review]","s":"reviewed"},{"i":"AC coverage trace [impl-review]","s":"reviewed"},{"i":"UI conformance [design-review — `n/a: outside phase gate` without a ux-spec/design-system draft; impl-review — conditional on a UI surface in scope]","s":"reviewed"}]}
```

## Verdict
APPROVE

## Next action
Reviewer done. Correctness is APPROVE-latched for wave 2.

## Escalations (optional)
