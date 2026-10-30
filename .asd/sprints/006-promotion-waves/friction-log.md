---
responsibility:
  owns: per-sprint log of workflow friction — a rule, phase, gate, agent, skill, template or provider tool that malfunctioned or could not be followed
  excludes: code defects (test-plan.md D-N), artifact-quality findings and verdicts (reviews/), human operational actions (manual-steps.md MS-N), decisions taken (decisions-log.md)
  delegates_to: test-plan.md (defects), reviews/ (verdicts), manual-steps.md (manual actions), decisions-log.md (decisions), retrospective.html (analysis and recommendations)
---

# Friction log — sprint 006-promotion-waves

<!--
Lifecycle, what qualifies, what never does, the F-N id scheme and who appends:
.asd/rules/sprint-lifecycle.md "Friction log" — not restated here.
Entry content is language.docs.
Consumed by the retro phase (.asd/workflows/asd-phase-retro.md).
-->

## Summary

| ID | Phase | Problem | Refs |
|---|---|---|---|
| F-1 | impl-test | Тестировщик ослабил тест бюджета SQL-команд стола (25 → 30), вместо того чтобы записать дефект кода | D-1 |
| F-2 | impl-review | Внешнее ревью недоступно: модель Codex перегружена, итерация засчитана пропуском | reviews/impl/wave-1/iter-01/external |
| F-3 | impl-review | Кэш доступности внешнего ревью `.asd/project/external-cache.json` не исключён в `.gitignore` | — |
| F-4 | impl-test | Тестировщик не записал операции прогона набора в журнал времени | test-plan.md Entry log |
| F-5 | impl-review | Внешнее ревью снова недоступно: исчерпан лимит Codex, итерация засчитана пропуском | reviews/impl/wave-1/iter-02/external, F-2 |
| F-6 | impl-review | Третья итерация подряд без внешнего ревью: отрицательный кэш quota | reviews/impl/wave-1/iter-03/external, F-2, F-5 |

## F-1 — Тестировщик ослабил тест бюджета SQL-команд стола, вместо того чтобы записать дефект кода

- **Phase**: impl-test
- **Surface**: agent — `asd-tester` (impl-test, запись 1)
- **What happened**: `DeskQueryCountTests` упал на 26 командах при бюджете 25. Тестировщик поднял `CommandBudget` до 30 и вернул это как отмеченный выбор. Бюджет 25 прямо записан в `docs/architecture/desk.md`, а `asd-phase-impl-test.md` требует считать такой красный тест дефектом кода.
- **Impact**: потребовался повторный диспатч тестировщика (вернуть бюджет, записать D-1) и отдельный круг impl test-fix и impl-test.
- **Refs**: D-1

## F-2 — Внешнее ревью недоступно: модель Codex перегружена

- **Phase**: impl-review
- **Surface**: provider tool — Codex CLI (`codex exec --model sol`), `asd-external-review`
- **What happened**: обе попытки, первая и повтор, завершились с кодом 1 и ошибкой «Selected model is at capacity». Провайдер не сообщил время сброса, поэтому записан отрицательный кэш `quota` на час, и в wave-1/iter-01 засчитан пропуск по доступности. Все 398 файлов остаются в `Unreviewed files`.
- **Impact**: итерация wave-1/iter-01 прошла без внешнего ревью. Непроверенные файлы переходят во внешнее ревью следующей итерации. Около 377 тыс. токенов Codex потрачено без вердикта.
- **Refs**: reviews/impl/wave-1/iter-01/external

## F-3 — Кэш внешнего ревью не исключён в `.gitignore`

- **Phase**: impl-review
- **Surface**: rule — `.asd/rules/external-review.md` «Detection and negative cache» (кэш «gitignored, machine-local»); в проекте `.gitignore` его не исключает
- **What happened**: после `external-record-failure` файл `.asd/project/external-cache.json` появился в `git status` как неотслеживаемый. Вход в impl-review требует чистого дерева. Оркестратор исключил файл локально через `.git/info/exclude` и не коммитит его.
- **Impact**: без локального исключения следующий вход в impl-review упал бы на проверке чистого дерева. На другой машине проблема повторится.
- **Refs**: —

## F-4 — Тестировщик не записал операции прогона набора в журнал времени

- **Phase**: impl-test
- **Surface**: agent — `asd-tester` (impl-test, запись 3); правило `asd-phase-impl-test.md` `## Rules` «bracket each suite run (steps 3, 8) with a `suite` op»
- **What happened**: запись 3 прогнала полный backend и веб-набор до и после написания тестов, но в `timing.jsonl` не появилось ни одной операции `suite`. Вердикт пришлось подтверждать повторным прогоном новых тестов оркестратором.
- **Impact**: время прогонов записи 3 потеряно для ретро, плюс около двух минут на повторный прогон.
- **Refs**: test-plan.md Entry log, строка 3

## F-5 — Внешнее ревью снова недоступно: исчерпан лимит Codex

- **Phase**: impl-review
- **Surface**: provider tool — Codex CLI (`codex exec --model sol`), `asd-external-review`
- **What happened**: в wave-1/iter-02 preflight был `local-ready`, но реальный запрос упал: первая попытка с кодом 1, повтор с отказом «You've hit your usage limit ... try again at 3:21 PM». Записан отрицательный кэш `quota` (сброс 15:21 MSK, ограничен часом), и итерация засчитана пропуском по доступности.
- **Impact**: вторая итерация подряд без внешнего ревью. Все 419 файлов, включая 398 перенесённых из iter-01, переходят в `Unreviewed files` следующей итерации. Около 56 тыс. токенов обёртки потрачено без вердикта.
- **Refs**: reviews/impl/wave-1/iter-02/external, F-2

## F-6 — Третья итерация подряд без внешнего ревью

- **Phase**: impl-review
- **Surface**: provider tool — Codex CLI, `asd-external-review`; правило `.asd/rules/external-review.md` «Detection and negative cache» (кэш ограничен часом, а провайдер назвал сброс позже)
- **What happened**: в wave-1/iter-03 preflight вернул `negative-cache` (quota до 11:46Z, провайдер назвал сброс в 15:21 MSK, то есть 12:21Z). Итерация засчитана пропуском по доступности без попытки вызова.
- **Impact**: ни одна итерация impl-review этого спринта пока не прошла внешнее ревью. 421 файл переходит в `Unreviewed files`. Если внутренний ревьюер даст APPROVE, фаза закроется без внешнего вердикта.
- **Refs**: reviews/impl/wave-1/iter-03/external, F-2, F-5
