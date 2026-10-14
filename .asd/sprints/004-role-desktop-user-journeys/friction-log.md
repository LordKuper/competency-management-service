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
| F-3 | design-review | asd-ux выполнил git stash в рабочем дереве оркестратора | — |
| F-4 | impl | asd-dev-critical перемещал чужие файлы в %TEMP% вне репозитория | — |

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

## F-3 — asd-ux выполнил git stash в рабочем дереве оркестратора

- **Phase**: design-review
- **Surface**: agent — asd-ux (autofix iter-03)
- **What happened**: при точечной правке ux-spec.html агент выполнил `git stash` (по своим словам, по ошибке) и сразу `git stash pop`; в дереве были незакоммиченные изменения оркестратора (timing.jsonl).
- **Impact**: потерь нет (проверено: stash пуст, изменения на месте); риск потери незакоммиченной бухгалтерии спринта при неудачном pop — у креатора нет причины трогать состояние git.
- **Refs**: —

## F-4 — asd-dev-critical перемещал чужие файлы в %TEMP% вне репозитория

- **Phase**: impl
- **Surface**: agent — asd-dev-critical (Task 5)
- **What happened**: в начале работы агент по ошибке переместил чужие `.txt` из корня `%TEMP%` в scratchpad и сразу вернул их (по его отчёту); в репозиторий это не попало.
- **Impact**: риск порчи файлов пользователя вне репозитория; повторяет F-3 (креатор выполняет файловые операции вне своей области).
- **Refs**: —

## F-5 — смена схемы impl-review посреди спринта не предусмотрена процессом

- **Phase**: impl
- **Surface**: workflow — `sprint-lifecycle.md` «Workflows» (workflow заморожен в `state.json.workflow`, не меняется посреди спринта)
- **What happened**: на приёмке реализации пользователь попросил перевести спринт на lite; правила запрещают менять замороженный workflow. Пользователь решил провести impl-review по схеме lite — одним общим ревьюером (`combined`) — при сохранении `workflow: standard`.
- **Impact**: отступление от замороженного состава `reviewers.impl` записано решением пользователя; у процесса нет штатного пути сузить состав ревьюеров для активного спринта (например, при росте поправок скоупа на приёмке).
- **Refs**: decisions-log 2026-10-14 «impl-review по схеме lite»

## F-6 — asd-dev убивал все процессы chrome.exe после визуальных проверок

- **Phase**: impl (impl assessment)
- **Surface**: agent — asd-dev (Task 19 assessment-fix 1, 2 и расширение колонки)
- **What happened**: после трёх headless-рендеров агент выполнял `taskkill //F //IM chrome.exe`, который принудительно закрывает все экземпляры Chrome на машине, включая открытый пользователем браузер.
- **Impact**: пользователь потерял открытый браузер; пользователь запретил скриншоты UI и закрытие своего браузера (память оркестратора, asd-dev, asd-ux). Повторяет F-3/F-4: креатор выполняет системные операции вне своей области.
- **Refs**: decisions-log 2026-10-14 «скриншоты запрещены»
