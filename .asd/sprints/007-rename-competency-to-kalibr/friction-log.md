---
responsibility:
  owns: per-sprint log of workflow friction — a rule, phase, gate, agent, skill, template or provider tool that malfunctioned or could not be followed
  excludes: code defects (test-plan.md D-N), artifact-quality findings and verdicts (reviews/), human operational actions (manual-steps.md MS-N), decisions taken (decisions-log.md)
  delegates_to: test-plan.md (defects), reviews/ (verdicts), manual-steps.md (manual actions), decisions-log.md (decisions), retrospective.html (analysis and recommendations)
---

# Friction log — sprint 007-rename-competency-to-kalibr

<!--
Lifecycle, what qualifies, what never does, the F-N id scheme and who appends:
.asd/rules/sprint-lifecycle.md "Friction log" — not restated here.
Entry content is language.docs.
Consumed by the retro phase (.asd/workflows/asd-phase-retro.md).
-->

## Summary

| ID | Phase | Problem | Refs |
|---|---|---|---|
| F-1 | impl-review | `runtime.js` передаёт git все пути диффа одной командой и падает с `ENAMETOOLONG` на Windows | — |

## F-1 — `runtime.js` передаёт git все пути диффа одной командой и падает с `ENAMETOOLONG` на Windows

- **Phase**: impl-review
- **Surface**: provider tool — `.asd/runtime.js` `rangePatchInvocations`/`patchPaths` (`review-waves`, `emit-manifest`)
- **What happened**: На входе 1 шаг 1c `review-waves --single` над 693 путями (32 162 символа) собрал один `spawnSync('git', ['--literal-pathspecs', 'diff', …, '--', <пути + источники переименований>])` примерно на 60 000 символов. Windows CreateProcess ограничивает командную строку 32 767 символами, отсюда `spawnSync git ENAMETOOLONG`. Тот же путь проходит `emit-manifest` шага 6, так что ни манифесты, ни дифф для ревьюеров не строятся. Обхода на Windows без правки инфраструктуры нет.
- **Impact**: impl-review остановлен до решения пользователя: консультация advisor и вопрос пользователю. По выбору пользователя `review-waves` и `emit-manifest` выполняются тем же неизменённым `runtime.js` в контейнере `node:24.21.0-trixie` (загрузка образа). Нужен upstream-фикс: `--pathspec-from-file` или разбиение вызовов git на части.
- **Refs**: —
