[REVIEW-impl-external]: FAIL

# External Review Report

- **Phase**: impl-review
- **Iteration**: wave-1/iter-01
- **Severity floor (this iter)**: low

## Kept findings

| # | Severity | Location | Description | Suggested fix |
|---|---|---|---|---|
| 1 | critical | src/Competency.OrgStructure/Import/ImportResolver.cs:47 | codex id=1 (blocker→critical): a ~87 KB workbook with 9999 levels of 200-character names passes upload/archive limits, but retained path keys alone require ~20 GB, letting an HR user exhaust server memory during preview. | Reject excessive path depth and cumulative path size before planning, and add a small deeply-nested workbook regression test. |
| 2 | high | src/Competency.OrgStructure/Import/OrgImportParser.cs:25 | codex id=2 (major): stored employee names retain repeated internal whitespace while workbook names are collapsed; stored "Али  оглы" does not match "Али оглы", so import creates a second employee and defaults the original to dismissal, potentially blocking that employee's account. | Apply ImportText.Clean before computing PersonName.Key and add a regression test for stored names with repeated whitespace. |
| 3 | medium | web/src/features/desk/OrgFinding.tsx:98 | codex id=3 (minor): HR now receives missing-head/missing-position findings, but their action links to ConnectUnitPage, still protected by AdminOnly, so following it lands HR on the not-found page. | Send HR users to the editable org-structure unit card and cover HR-only finding navigation. |
| 4 | medium | src/Competency.OrgStructure/Import/OrgImportEndpoints.cs:177 | codex id=4 (minor): JSON arrays containing null elements are accepted; decisions=[null] or heads=[null] with a valid fingerprint causes a null dereference in resolution and returns 500 instead of a validation error. | Reject null list elements and add regression cases for both fields. |

## Dropped findings (counts only)

- Below severity floor (iter 1, floor low): 0
- Nitpick, by category: none: 0

## Verdict
FAIL: 4

## Next action
Route all four findings to impl review-fix: critical memory exhaustion (1), high name-matching defect (2), and medium findings (3, 4). Tests were not run by the wrapped CLI.

approved change (user, 2026-10-27): finding 1 — accepted for fix as a depth cap of 30 unit levels, checked on the header (`OrgImportParser.ReadHeader`), error «В файле больше 30 уровней подразделений.»; AC-11 wording amended to «не более 30 уровней» as a technical parse limit under AC-10. Findings 2, 3, 4 — accepted for fix as suggested (3: HR goes to the org-structure unit card `?unit=<id>`, admin keeps ConnectUnitPage; both link sites OrgFinding.tsx and deskTexts.ts).
