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
- 2026-10-13 — design-review iter-01 вопрос ревьюера correctness #1 (user): при публикации, которую запускает не HR («Принять правки» после возврата от HR; согласование шага 1 при автоматическом шаге 2), право утвердить черновики каскада проверяется у ответственного HR подразделения матрицы, чей шаг засчитывается согласованным; инициатор видит перечень каскада и подтверждает его, в т.ч. на шаге 1 (закрывает и external #1)
- 2026-10-13 — design-review iter-01: correctness CONCERNS (6), efficiency CONCERNS (2), documentation CONCERNS (6), external CONCERNS (5); FAIL нет → autofix: asd-architect, затем asd-ba, затем asd-ux
- 2026-10-13 — design-review iter-01 autofix: asd-architect (adr.html, c4-full), asd-ba (prd.html), asd-ux (ux-spec.html, новый design-md-delta.yaml); оркестратор выставил STATUS in-review у черновиков (artifact-layout.md `{{STATUS}}`)
- 2026-10-13 — design-md-delta.yaml: две документационные записи без новых токенов и без смены значений (контраст accent на primary 3.73:1 для счётчика; горизонтальный Steps) — адаптивно (оркестратор) как механическое обновление дизайн-системы (checkpoints.md, routine в adaptive)
- 2026-10-13 — design-review iter-02 ответы пользователя: (1) вопрос documentation #1 — вариант b: «Принять правки» публикует и показывает перечень каскада, когда после него шаг HR согласуется автоматически (автор — ответственный HR); список публикующих действий — только в AC-34.4 и ADR-4 `publishingActions`; (2) external #1 — запись, создаваемая сразу действующей со ссылками на черновики, создаётся черновиком, окно сразу предлагает «Утвердить» с перечнем каскада; без ссылок на черновики — действующей, как сейчас; нового метода предпросмотра нет. Стоимость AC-34: 2 итерации (нижняя граница) / 1 раунд исправлений
- 2026-10-13 — design-review iter-02: correctness CONCERNS (3), efficiency APPROVE (latched), documentation CONCERNS (4), external CONCERNS (3) → autofix: asd-architect, asd-ba, asd-ux
- 2026-10-13 — design-review iter-02 autofix: asd-architect (adr.html), asd-ba (prd.html, новый AC-34.10), asd-ux (ux-spec.html, design-md-delta.yaml с полем doc_patch вне шаблона — формат подтверждается на design-promote)
- 2026-10-13 — design-review iter-03 ответы пользователя: (1) FAIL external #1 (гонка предпросмотра и публикации) — принято исправление: переход версии, который фактически публикует, без совпадающего тега каскада получает 412, экран заново показывает перечень для подтверждения; правило попадает в изменения поведения 3.1.0 (отклонены: тег с маршрутом и ответственным HR; принятие риска); (2) вопрос correctness #1 — правка действующей записи, назначающая черновую ссылку, отклоняется 400 с ошибкой поля; замыкание версии проходит и через действующие компетенции к их черновым ссылкам. Стоимость AC-34: 3 итерации (нижняя граница) / 2 раунда исправлений
- 2026-10-13 — design-review iter-03: correctness CONCERNS (1), documentation APPROVE (latched), efficiency latched, external FAIL (1) → исправления: asd-architect, asd-ba, asd-ux
- 2026-10-13 — design-review iter-03 исправления: asd-architect (adr.html: 412 для любого публикующего действия без совпадающего тега, в т.ч. записи с непустым каскадом; PUT 400; замыкание через действующие компетенции), asd-ba (prd.html: AC-34.2, 34.5, 34.7, новый AC-34.11), asd-ux (ux-spec.html: состояние 3б, ошибка поля AC-34.11)
