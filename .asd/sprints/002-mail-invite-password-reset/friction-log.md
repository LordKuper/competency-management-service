---
responsibility:
  owns: per-sprint log of workflow friction — a rule, phase, gate, agent, skill, template or provider tool that malfunctioned or could not be followed
  excludes: code defects (test-plan.md D-N), artifact-quality findings and verdicts (reviews/), human operational actions (manual-steps.md MS-N), decisions taken (decisions-log.md)
  delegates_to: test-plan.md (defects), reviews/ (verdicts), manual-steps.md (manual actions), decisions-log.md (decisions), retrospective.html (analysis and recommendations)
---

# Friction log — sprint 002-mail-invite-password-reset

<!--
Lifecycle, what qualifies, what never does, the F-N id scheme and who appends:
.asd/rules/sprint-lifecycle.md "Friction log" — not restated here.
Entry content is language.docs.
Consumed by the retro phase (.asd/rules/sprint-lifecycle.md "Retro phase").
-->

## Summary

| ID | Phase | Problem | Refs |
|---|---|---|---|
| F-1 | impl-test | Правило поправки скоупа не описывает новую Task при выходе impl-test в test-fix | D-1, D-2 |

## F-1 — Правило поправки скоупа не описывает новую Task при выходе impl-test в test-fix

- **Phase**: impl-test
- **Surface**: rule — `.asd/rules/sprint-lifecycle.md` "Scope amendment", `.asd/workflows/asd-phase-impl.md` step 11
- **What happened**: Поправка AC-17 (Task 7, волна 6) принята во время impl-test entry 1; entry 1 вернул `D-1`, `D-2` в impl test-fix. Правило «одна запись impl выполняет два режима» сформулировано только для `review_fixes_pending`; для test-fix шаг 11 не предписывает продолжить initial-режим по неотмеченным Task. Оркестратор применил тот же порядок по аналогии (test-fix, затем Task 7 initial, затем impl assessment) и записал это в decisions-log.
- **Impact**: решение по аналогии вместо правила; риск, что Task 7 осталась бы неотмеченной до impl-review.
- **Refs**: D-1, D-2
