---
responsibility:
  owns: approved decisions for THIS sprint
  excludes: cross-sprint/durable decisions, sprint state, review notes
  delegates_to: docs/** + adr fold targets (durable design decisions), CHANGELOG.md (releases), .asd/project/stubs.md (standing open defects), .asd/project/retro-backlog.md (retro row dispositions), state.json (state), reviews/ (verdicts)
---

# Decisions Log

Per-sprint, append-only. Never edited or removed. Created at `scope`, rotated at phase entry (`.asd/rules/artifact-layout.md` "Decisions log"), archived with the sprint.

## Entry format

```markdown
## YYYY-MM-DD — <one-line summary>

- **Decision**: <what was decided> (≤3 sentences)
- **Rationale**: <why> (≤3 sentences)
- **Affected docs**: <links> (unrestricted)
```

A no-op skip, other zero-content decision, dispatch routing line or failed-dispatch reconstruction uses the one-line form instead:

```markdown
- YYYY-MM-DD — <phase> skipped: <reason>
- YYYY-MM-DD — route <taskIds>: <tier>, dispatch HEAD <sha>[; risk <declaration>]
- YYYY-MM-DD — reconstruction: landed <ids>; re-dispatched <ids>
- YYYY-MM-DD — stall: <agent> <dispatch ids>
```

## Durability rule

A decision whose value must survive this sprint's archival is ALSO written into an existing persistent home — a `docs/` fold target, `CHANGELOG.md`, `.asd/project/stubs.md`, or `.asd/project/retro-backlog.md` (retro row dispositions). Never invent a new document type for this. This log records that the decision was made; the persistent home is what a later sprint can still read.

## Entries

<!-- entries appended below this line -->
- 2026-10-09 — route Task 1: standard, dispatch HEAD a07cf4a
- 2026-10-09 — route Task 2: critical, dispatch HEAD a07cf4a; risk change: migration, change: public contract

## 2026-10-09 — Task 2 flagged choices

- **Decision**: Приняты: пункт «Добавить должность «…»» — обычная опция списка, а не `popupRender` (доступность с клавиатуры, accessibility.html); счётчики перед деактивацией и исключением — только работающие сотрудники; производные тексты ошибок по образцу «Не удалось …», столбец «Действия», aria-label из утверждённого заголовка, «Должность не найдена»; форма API (204 на повторное добавление, `GET /positions/{id}`, quick-add 200 без требования активности подразделения). Пункт меню «Должности» сейчас только администратору — Task 6 открывает просмотр перечня всем вместе с пунктом «HR». Не принят п. 5: полнотекстовый запрос, смешивающий слово ФИО и слово должности, перестал находить сотрудника — нарушение AC-5 «как прежде»; возврат Task 2 на исправление.
- **Rationale**: Оценка оркестратора по AC-2, AC-5, AC-20 и accessibility.html; производные тексты не меняют утверждённых формулировок.
- **Affected docs**: plan.md Task 2, Task 6
- 2026-10-09 — route Task 2: critical, dispatch HEAD f259005 (re-dispatch, priorTier critical)

## 2026-10-09 — plan revision: Test-only Task 10

- **Decision**: В plan добавлена Test-only Task 10 (волна 2, `asd-tester`): фикстуры `Employee` в `EmployeeCards.test.tsx` и `orgTree.test.ts` под контракт 3.0.0; критерии и прочие Task не меняются.
- **Rationale**: Смена входа `/api/v1/employees` (Task 2, AC-6) ломает typecheck web-тестов, а он входит в `lint`/`build` gate impl; dev не правит тесты. Адаптивное решение оркестратора: объём — две фикстуры, проверка — `npm --prefix web run typecheck`.
- **Affected docs**: plan.md
- 2026-10-09 — Task 2 follow-up accepted (cc0e98d): смешанный запрос ФИО+должность снова находит сотрудника; полнотекстовый путь без GIN-индекса (конкатенация векторов) принят — перф-бюджет org-structure AC-24 (P95 < 2 с на 10 000) проверяется в impl-test
- 2026-10-09 — route Task 3: critical, dispatch HEAD f51aca9; risk change: lock ordering
- 2026-10-09 — route Task 10: standard, dispatch HEAD f51aca9; risk none via npm --prefix web run typecheck
- 2026-10-09 — Task 3 flagged choices accepted: `FindAncestryAsync` включает само подразделение первым (шаг 1 маршрута пропускает его), защита от цикла `CYCLE … USING path`, `FindHeadedUnitsAsync` без фильтра активности (фильтрует вызывающий), «действующая учётная запись» — один раз в `UserStatus.IsActive`; Task 10 COMPLETED (eca4c33), web typecheck и lint чисты
