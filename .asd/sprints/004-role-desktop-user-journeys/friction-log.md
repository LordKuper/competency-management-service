---
responsibility:
  owns: per-sprint log of workflow friction — a rule, phase, gate, agent, skill, template or provider tool that malfunctioned or could not be followed
  excludes: code defects (test-plan.md D-N), artifact-quality findings and verdicts (reviews/), human operational actions (manual-steps.md MS-N), decisions taken (decisions-log.md)
  delegates_to: test-plan.md (defects), reviews/ (verdicts), manual-steps.md (manual actions), decisions-log.md (decisions), retrospective.html (analysis and recommendations)
---

# Friction log — sprint 004-role-desktop-user-journeys

<!--
Lifecycle, what qualifies, what never does, the F-N id scheme and who appends:
.asd/rules/sprint-lifecycle.md "Friction log" — not restated here.
Entry content is language.docs.
Consumed by the retro phase (.asd/rules/sprint-lifecycle.md "Retro phase").
-->

## Summary

| ID | Phase | Problem | Refs |
|---|---|---|---|
| F-1 | design-review | asd-reviewer-documentation без Edit переписывает файл памяти целиком | reviews/design/iter-01/documentation |
| F-2 | design-review | ревьюер прочитал отчёт прошлой итерации вопреки clean-context | reviews/design/iter-02/correctness |

## F-1 — asd-reviewer-documentation без Edit переписывает файл памяти целиком

- **Phase**: design-review
- **Surface**: agent — `.claude/agents/asd-reviewer-documentation.md` (tools: Read, Glob, Grep, Write)
- **What happened**: ревьюер добавил запись в свою память и, не имея Edit, перезаписал `.claude/agent-memory/asd-reviewer-documentation/MEMORY.md` целиком (итог — +1 строка, потерь нет); так же устроены остальные ревьюеры с одним Write.
- **Impact**: риск потери строк индекса памяти при каждой записи; противоречит включённому в спринт правилу AC-23 (точечная правка памяти), которое ревьюер с таким набором инструментов выполнить не может.
- **Refs**: —

## F-2 — ревьюер прочитал отчёт прошлой итерации вопреки clean-context

- **Phase**: design-review
- **Surface**: agent — asd-reviewer-correctness; rule `.asd/rules/review-policy.md` «Clean-context review iteration»
- **What happened**: на iter-02 ревьюер в начале проверки прочитал `reviews/design/iter-01/correctness.md` (сам указал это в отчёте); payload путей прошлых отчётов не содержал, папка ревью соседняя и доступна чтению.
- **Impact**: риск смещения вердикта к прошлым находкам; по словам ревьюера, находки выведены из diff и черновиков, повторов нет — итерация принята.
- **Refs**: reviews/design/iter-02/correctness; повторилось на reviews/design/iter-03/correctness
