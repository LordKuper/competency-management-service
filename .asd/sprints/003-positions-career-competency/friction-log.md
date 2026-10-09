---
responsibility:
  owns: per-sprint log of workflow friction — a rule, phase, gate, agent, skill, template or provider tool that malfunctioned or could not be followed
  excludes: code defects (test-plan.md D-N), artifact-quality findings and verdicts (reviews/), human operational actions (manual-steps.md MS-N), decisions taken (decisions-log.md)
  delegates_to: test-plan.md (defects), reviews/ (verdicts), manual-steps.md (manual actions), decisions-log.md (decisions), retrospective.html (analysis and recommendations)
---

# Friction log — sprint 003-positions-career-competency

<!--
Lifecycle, what qualifies, what never does, the F-N id scheme and who appends:
.asd/rules/sprint-lifecycle.md "Friction log" — not restated here.
Entry content is language.docs.
Consumed by the retro phase (.asd/rules/sprint-lifecycle.md "Retro phase").
-->


| ID | Phase | Problem | Refs |
|---|---|---|---|
| F-1 | audit | Архитектор записал файл своей памяти, хотя payload аудита требовал только чтение | — |
| F-2 | audit | Ротация журнала решений перед делегированием audit выполнена после запуска скилла аудита, а не до | — |

## F-1 — Архитектор записал файл своей памяти, хотя payload аудита требовал только чтение

- **Phase**: audit
- **Surface**: agent — `asd-architect` (audit dispatch), `.claude/agent-memory/asd-architect/feedback_audit-contradiction-classification.md`, `MEMORY.md`
- **What happened**: Payload диспатча указывал «Read-only: no file writes»; агент добавил заметку в собственную память и сам сообщил об этом в отчёте.
- **Impact**: Лишняя запись вне payload, которую оркестратор проверяет (`memory-check`) и коммитит; запись не противоречит правилам.
- **Refs**: —

## F-2 — Ротация журнала решений выполнена после запуска скилла аудита

- **Phase**: audit
- **Surface**: skill — `asd-sprint` Workflow «Before a phase-skill delegation below, rotate the decisions log»
- **What happened**: Оркестратор вызвал `asd-phase-audit` до ротации `decisions-log.md`; ротацию сделал первым действием внутри фазы, до любой записи аудита.
- **Impact**: Порядок нарушен, содержимое не пострадало; повторной работы не было.
- **Refs**: —
