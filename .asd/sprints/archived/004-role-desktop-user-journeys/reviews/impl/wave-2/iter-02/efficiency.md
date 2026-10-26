[REVIEW-impl-efficiency]: CONCERNS

# Review — efficiency

- **Phase**: impl-review
- **Iteration**: wave-2/iter-02

## Findings

| # | Severity | Location | Description | Suggested fix | Category |
|---|---|---|---|---|---|
| 1 | critical | web/src/features/desk/TrackPositionsPage.tsx:92 | Обработчик `onOpenChange` выбора трека сбрасывает поисковый текст при закрытии списка, но одиночный `Select` делает это сам: эффект `BaseSelect` в `@rc-component/select` (по `rawOpen`, для режима не multiple и не combobox) вызывает `onSearch('')` при закрытии, а `onSearch` здесь тот же `setSearch`. Удаление обработчика ничего не меняет (мутант `T2b` в `test-plan.md` выжил), то есть это защитный код на случай, невозможный по контракту библиотеки. Пункт чек-листа: «Defensive code for impossible-by-contract case». | Удалить `onOpenChange` целиком (строки 92-94); `showSearch={{ filterOption: false, onSearch: setSearch }}` и тест «после закрытия списка» остаются как есть. Одноимённые обработчики в `CatalogSelect`, `DirectionSelect`, `ScaleSelect`, `MatrixPositionSelect`, `PositionSelect` вне изменённой поверхности итерации, их не трогать. | simplify |

## Coverage (internal reviewers only)

Ledger привязан к дайджесту manifest `38bed24707a3d2a503e7082edb080ed0bfc27994b4d98cc474b270975eed03d7`. Раздел «Perf budget compliance» и одноимённое правило помечены `n/a: no budgets defined`: в `custom-coding-rules.md` нет раздела с бюджетами, а замер делается только для Task с заявленным риском производительности (такого в дельте нет).

```json
{
  "manifest_digest": "38bed24707a3d2a503e7082edb080ed0bfc27994b4d98cc474b270975eed03d7",
  "findings": ["1"],
  "files": [
    {"i": ".claude/agent-memory/asd-dev-critical/MEMORY.md", "s": "checked"},
    {"i": ".claude/agent-memory/asd-dev-critical/feedback_commit-subject-length.md", "s": "checked"},
    {"i": ".claude/agent-memory/asd-reviewer-testing/MEMORY.md", "s": "checked"},
    {"i": ".claude/agent-memory/asd-reviewer-testing/feedback_web-spa-test-review.md", "s": "checked"},
    {"i": ".claude/agent-memory/asd-tester-critical/MEMORY.md", "s": "checked"},
    {"i": ".claude/agent-memory/asd-tester-critical/project_competency-test-harness.md", "s": "checked"},
    {"i": ".claude/agent-memory/asd-tester/project_web-antd-test-quirks.md", "s": "checked"},
    {"i": "web/src/features/desk/AccountModal.tsx", "s": "checked"},
    {"i": "web/src/features/desk/AddBlockModal.test.tsx", "s": "checked"},
    {"i": "web/src/features/desk/AddBlockModal.tsx", "s": "checked"},
    {"i": "web/src/features/desk/BuilderCheckStep.tsx", "s": "checked"},
    {"i": "web/src/features/desk/BuilderCompetenciesStep.test.tsx", "s": "checked"},
    {"i": "web/src/features/desk/BuilderCompetenciesStep.tsx", "s": "checked"},
    {"i": "web/src/features/desk/BuilderSubmitStep.test.tsx", "s": "checked"},
    {"i": "web/src/features/desk/BuilderSubmitStep.tsx", "s": "checked"},
    {"i": "web/src/features/desk/BuilderTrackStep.tsx", "s": "checked"},
    {"i": "web/src/features/desk/CatalogCleanupPage.tsx", "s": "checked"},
    {"i": "web/src/features/desk/ChangePositionForm.tsx", "s": "checked"},
    {"i": "web/src/features/desk/ConnectUnitPage.test.tsx", "s": "checked"},
    {"i": "web/src/features/desk/ConnectUnitPage.tsx", "s": "checked"},
    {"i": "web/src/features/desk/CreateAccountForm.tsx", "s": "checked"},
    {"i": "web/src/features/desk/DeskCounter.test.tsx", "s": "checked"},
    {"i": "web/src/features/desk/DeskGroups.tsx", "s": "checked"},
    {"i": "web/src/features/desk/DeskPage.test.tsx", "s": "checked"},
    {"i": "web/src/features/desk/DeskScenarios.test.tsx", "s": "checked"},
    {"i": "web/src/features/desk/DeskScenarios.tsx", "s": "checked"},
    {"i": "web/src/features/desk/DraftsQueuePage.test.tsx", "s": "checked"},
    {"i": "web/src/features/desk/DraftsQueuePage.tsx", "s": "checked"},
    {"i": "web/src/features/desk/LinkButton.test.tsx", "s": "checked"},
    {"i": "web/src/features/desk/LinkButton.tsx", "s": "checked"},
    {"i": "web/src/features/desk/MatrixBuilderPage.test.tsx", "s": "checked"},
    {"i": "web/src/features/desk/MatrixBuilderPage.tsx", "s": "checked"},
    {"i": "web/src/features/desk/MyPlacePage.test.tsx", "s": "checked"},
    {"i": "web/src/features/desk/MyPlacePage.tsx", "s": "checked"},
    {"i": "web/src/features/desk/OnboardEmployeePage.test.tsx", "s": "checked"},
    {"i": "web/src/features/desk/OnboardEmployeePage.tsx", "s": "checked"},
    {"i": "web/src/features/desk/OrgCleanupPage.test.tsx", "s": "checked"},
    {"i": "web/src/features/desk/OrgCleanupPage.tsx", "s": "checked"},
    {"i": "web/src/features/desk/OrgFinding.tsx", "s": "checked"},
    {"i": "web/src/features/desk/QueueLayout.tsx", "s": "checked"},
    {"i": "web/src/features/desk/ReviewPage.test.tsx", "s": "checked"},
    {"i": "web/src/features/desk/ReviewPage.tsx", "s": "checked"},
    {"i": "web/src/features/desk/ReworkPage.test.tsx", "s": "checked"},
    {"i": "web/src/features/desk/ReworkPage.tsx", "s": "checked"},
    {"i": "web/src/features/desk/StepRuler.tsx", "s": "checked"},
    {"i": "web/src/features/desk/TrackPositionsPage.test.tsx", "s": "checked"},
    {"i": "web/src/features/desk/TrackPositionsPage.tsx", "s": "checked"},
    {"i": "web/src/features/desk/builderContent.test.ts", "s": "checked"},
    {"i": "web/src/features/desk/deskTexts.test.ts", "s": "checked"},
    {"i": "web/src/features/desk/scenario.css", "s": "checked"},
    {"i": "web/src/features/desk/useResendInvitation.ts", "s": "checked"},
    {"i": "web/src/features/desk/useVersionTransition.tsx", "s": "checked"},
    {"i": "web/src/features/org-structure/EmployeeForm.tsx", "s": "checked"},
    {"i": "web/src/features/users/UserForm.tsx", "s": "checked"},
    {"i": "web/src/features/users/UserModal.test.tsx", "s": "checked"},
    {"i": "web/src/features/users/roles.test.ts", "s": "checked"},
    {"i": "web/src/features/users/userMail.test.tsx", "s": "checked"}
  ],
  "rules": [
    {"i": "Over-engineering checklist [design-review, impl-review] — critical, undroppable", "s": "finding", "f": "1"},
    {"i": "Structure / cohesion checklist [design-review, impl-review] — critical, undroppable", "s": "pass"},
    {"i": "Complexity-vs-value tradeoff [design-review, impl-review]", "s": "pass"},
    {"i": "Perf budget compliance [impl-review]", "s": "n/a", "p": "no budgets defined"},
    {"i": "Perf anti-patterns [impl-review]", "s": "pass"},
    {"i": "Algorithmic complexity [impl-review]", "s": "pass"},
    {"i": "Regression detection [impl-review]", "s": "pass"},
    {"i": "Hot path identification [impl-review]", "s": "pass"},
    {"i": ".asd/project/custom-common-rules.md", "s": "pass"},
    {"i": ".asd/project/custom-coding-rules.md", "s": "pass"}
  ],
  "sections": [
    {"i": "Over-engineering checklist [design-review, impl-review] — critical, undroppable", "s": "reviewed"},
    {"i": "Structure / cohesion checklist [design-review, impl-review] — critical, undroppable", "s": "reviewed"},
    {"i": "Complexity-vs-value tradeoff [design-review, impl-review]", "s": "reviewed"},
    {"i": "Perf budget compliance [impl-review]", "s": "n/a", "p": "no budgets defined"},
    {"i": "Perf anti-patterns [impl-review]", "s": "reviewed"},
    {"i": "Algorithmic complexity [impl-review]", "s": "reviewed"},
    {"i": "Regression detection [impl-review]", "s": "reviewed"},
    {"i": "Hot path identification [impl-review]", "s": "reviewed"}
  ]
}
```

## Verdict
CONCERNS: 1

## Next action
Разработчик в review-fix удаляет `onOpenChange` в `TrackPositionsPage.tsx:92-94`; видимое поведение не меняется, тест «после закрытия списка» остаётся зелёным. Исправление не добавляет абстракцию, слой или зависимость, эскалация не нужна.

Разобрано и не поднято (категория keep-as-is, вне таблицы):
- `setIsDirty(false)` в `onSuccess` у `ChangePositionForm` и `LinkAccount` (мутант G9 impl-test): при успешном сохранении и сбое последующего перечитывания очереди находка остаётся на экране, и без сброса охрана ушла бы спрашивать о уже сохранённом. Достижимо, один оператор.
- Проверка пересечения в `attach` в `BuilderCompetenciesStep.tsx` (мутант K2): достижима через создание блока в мастере, окно выбора блока её не заменяет.
- Убранный N+1 в `AddBlockModal` (чтения компетенций ради названия области) и удалённые `cancelLabel` и `clearError` не оставили потребителей.

## Escalations
Нет.
