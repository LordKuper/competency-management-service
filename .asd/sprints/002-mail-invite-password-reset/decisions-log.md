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

## 2026-10-08 — audit answers (Q-1…Q-4)

- **Decision**: Q-1 — анонимный «Не помню пароль» всегда отвечает нейтрально, отказ SMTP только в журнале и логе. Q-2 — действия администратора отправляют письмо синхронно после фиксации с коротким таймаутом, анонимный запрос — фоновая очередь в памяти (без outbox). Q-3 — `info.version` 2.0.0, путь `/api/v1` прежний. Q-4 — скрипт наполнения 10 000+ создаёт первая Task с perf-риском, в этом спринте нет.
- **Rationale**: Решения пользователя (рекомендованные варианты); затраты по AC-1, AC-9, AC-12: 0 итераций ревью, 0 раундов исправлений.
- **Affected docs**: sprint.md AC-1, AC-9, AC-12; audit.md Contradictions, Gaps

## 2026-10-08 — audit accepted (adaptive)

- **Decision**: audit.md принят адаптивно; почтовый перехватчик — Mailpit `axllent/mailpit:v1.31.4` (MIT, только разработка), tech-reference пишется до реализации.
- **Rationale**: Все жёсткие решения (противоречия, изменения AC) приняты пользователем; новых подсистем нет; выбор перехватчика делегирован пользователем в scope. Mailpit в интеграционных тестах (если plan его предложит) — решение plan.
- **Affected docs**: audit.md, sprint.md
