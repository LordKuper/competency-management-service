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
- YYYY-MM-DD — cleanup checkpoint <sha>
- YYYY-MM-DD — cleanup: removed <paths/symbols>; reverted <ids or none>; verified <build, lint, tests @sha>
```

## Durability rule

A decision whose value must survive this sprint's archival is ALSO written into an existing persistent home — a `docs/` fold target, `CHANGELOG.md`, `.asd/project/stubs.md`, or `.asd/project/retro-backlog.md` (retro row dispositions). Never invent a new document type for this. This log records that the decision was made; the persistent home is what a later sprint can still read.

## Entries

<!-- entries appended below this line -->

## 2026-10-30 — Память агентов: AC-11 → AC-18, AC-15 → AC-19

- **Decision**: AC-11 снят и заменён AC-18: владельцы `asd-dev` и `asd-tester` правят свою память сами (проверка оставшихся вхождений термина в impl-test, memory-fix); в каталогах удалённых агентов удаляются только 17 файлов с именами продукта и их строки индекса, доменные упоминания и прочие файлы остаются. AC-15 снят и заменён AC-19: полное правило строки `006-promotion-waves#A-5` (открытие и закрытие `suite`) для действующего `asd-tester`. Ответы пользователя, рекомендованные варианты.
- **Rationale**: `review-policy.md` «Memory-fix dispatch»: не владелец не пишет текст памяти, память удалённого агента только удаляется; `asd-tester-critical` — каталог удалённого агента, его память не загружается, поэтому сужение A-5 было неверным. Стоимость AC-18, AC-19: 0 итераций, 0 раундов исправлений.
- **Affected docs**: [sprint.md](./sprint.md)

- 2026-10-30 — stubs: none match scope (`audit.md` has no «Related open stubs»; `.asd/project/stubs.md` has no rows)

## 2026-10-30 — `.asd/sprints/007-rename-competency-to-kalibr/plan.md` accepted

- **Decision**: План принят оркестратором (adaptive): Task 1 (код, контракт, образ, web) и Task 2 (`deploy/**`) в волне 1; правила, `commands.yaml`, память агентов и `docs/**` — действия оркестратора в impl, impl-test и design-promote.
- **Rationale**: Каждая задача прослеживается к AC, одобренным пользователем, и к его ответам на scope и audit; изменений настроек, заглушек и новых зависимостей нет; нерешённых альтернатив нет.
- **Affected docs**: [plan.md](./plan.md)
