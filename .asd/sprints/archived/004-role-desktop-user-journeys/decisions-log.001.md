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

## 2026-10-12 — workflow спринта: standard

- **Decision**: спринт 004 идёт по workflow `standard` (решение пользователя, рекомендованный вариант).
- **Rationale**: спринт меняет навигацию и UX всего приложения и, вероятно, добавляет агрегирующий API; дизайн-черновики проходят ревью до кода (в спринте 003 на lite — шесть поправок скоупа во время impl).
- **Affected docs**: state.json

## 2026-10-12 — ответы на уточнение скоупа

- **Decision**: рабочий стол — домашняя страница и первый пункт меню, сценарии — собственные пошаговые экраны, табличные разделы остаются вторичным доступом; стол для ролей руководитель, HR, администратор, сотрудник; список ожидаемых действий и сценариев предлагает оркестратор; напоминания — счётчик в шапке и письма.
- **Rationale**: без этих ответов скоуп не конкретизировался (связь с табличными разделами, состав ролей, канал напоминаний).
- **Affected docs**: sprint.md
- 2026-10-12 — documents: audit=true (auto: скоуп меняет поведение и контракт), prd, ux_spec, adr — enabled в config; c4=true (diagram_tool mermaid, subsystem_decomposition enabled)

## 2026-10-12 — retro intake: проверка кандидатов на HEAD bc149c1

- **Decision**: `retro-candidates` вернул 6 строк к решению и 6 `upstream: true` (003#A-1, A-3, A-5, A-7, A-8, P-3 — только показ на gate). Все 6 строк к решению не выполнены на HEAD bc149c1: 003#A-4 — в `.claude/agent-memory/asd-dev-critical/` рецепты scratch-БД и API без пометки «только impl-test / tester» (`project_migration-scratch-verification-recipe.md`, `project_scratch-process-control.md`; `project_scratch-host-without-postgres.md` прямо предлагает проверку «before impl-test»); 003#A-6 и 003#P-2 — в `custom-coding-rules.md` нет правил сверки с таблицей компонентов DESIGN.md и блокировки/412 в UI-потоках; 003#P-1, 001#P-2, 002#A-2 — в `custom-common-rules.md` нет правил макетов на gate plan, одной подсистемы на спринт и точечной правки памяти.
- **Rationale**: правило «Retrospective-derived criteria are re-verified at scope»; закрытых без вопроса нет.
- **Affected docs**: .asd/project/retro-backlog.md (после решения на scope gate)

## 2026-10-12 — scope принят

- **Decision**: пользователь принял скоуп спринта 004: AC-1…AC-18 (рабочий стол, ожидаемые действия по ролям, сценарии, письма, сквозные требования) и AC-19…AC-23 из retro intake; деление на спринты отклонено, документы не пропускаются.
- **Rationale**: hard gate scope; ретро-строки 003#A-6, 003#P-2, 003#P-1, 003#A-4, 002#A-2 включены (стоимость каждой 0 итераций / 0 раундов исправлений — новые критерии), 001#P-2 отклонена навсегда; upstream-предложения 003#A-1, A-3, A-5, A-7, A-8, P-3 показаны без решения.
- **Affected docs**: sprint.md, .asd/project/retro-backlog.md, state.json
