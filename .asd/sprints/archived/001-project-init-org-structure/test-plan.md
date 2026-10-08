---
responsibility:
  owns: test approach for sprint change scope, removal reasons, no-test decisions, suite run result, code defects found by tests, manual-verification spec (single home — never duplicated in a review file)
  excludes: task breakdown, requirements, review verdicts, code, change surface (derivable from the diff)
  delegates_to: plan.md (tasks), persistent docs (requirements), reviews/impl/wave-<K>/iter-NN/testing.md (verdict)
---

# Test plan — sprint 001-project-init-org-structure

## Entry log

Appended each entry, never rewritten. `HEAD analysed` is the commit the strategy/prune passes
were scoped through; the next re-entry's delta is `git diff <this sha>...HEAD`.

| Entry | HEAD analysed | Scope |
|---|---|---|
| 1 | 82776210245b389c80e76088fc1cffa74a0f0579 | full change surface |
| 2 | 133c58c37107b0d4528f9fee2ee765d1e88e5e01 | delta since entry 1 (82776210245b389c80e76088fc1cffa74a0f0579) |
| 3 | 3ae1630d0671e0ae021b7308610c13fb5b367f8d | delta since entry 2 (133c58c37107b0d4528f9fee2ee765d1e88e5e01) |
| 4 | 08d21ccc83fa5cacc398b0fb94e75540dd18b50a | delta since entry 3 (3ae1630d0671e0ae021b7308610c13fb5b367f8d) |
| 5 | a460cdd91cb096209e06ba868f90574e1b8633e7 | delta since entry 4 (08d21ccc83fa5cacc398b0fb94e75540dd18b50a) |
| 6 | 7366455d6e0d2f2001ee1808e5dcae3d5c36021d | delta since entry 5 (a460cdd91cb096209e06ba868f90574e1b8633e7) |
| 7 | 5bcf82e348f1d475468254f29efdbf93f512bcff | delta since entry 6 (7366455d6e0d2f2001ee1808e5dcae3d5c36021d) |
| 8 | 995b1389880b9eb474be0dda5f1d79bd4d74718f | delta since entry 7 (5bcf82e348f1d475468254f29efdbf93f512bcff) |

## Risk → check decisions

Entry 8 — дельта `5bcf82e348f1d475468254f29efdbf93f512bcff...HEAD` (a817235) без `.asd/**`: review-fix wave-3/iter-02, dev 24a6eec (`web/src/app/useDebouncedValue.ts`: убран неиспользуемый параметр `delayMs`) и тестовый 0e54ba2 (`web/src/test/fakeApi.ts`: убран псевдоним `json`, вызывающие `authFlow.test.tsx`, `editDialogs.test.tsx`, `OrgStructurePage.test.tsx` берут `Response.json`). Строки entry 7 перенесены в `test-plan.entry-07.md`; их риски не изменились и в этом файле не повторяются.

| Change | Material risk | Chosen check | Decision | Reason |
|---|---|---|---|---|
| `useDebouncedValue`: параметр `delayMs` удалён (24a6eec); `fakeApi.json` удалён, тесты на `Response.json` (0e54ba2) | нового риска нет: параметр никто не передавал, задержка берётся из константы; псевдоним был тонкой обёрткой над `Response.json` | существующие тесты (задержку набора проходит `OrgStructurePage.test.tsx`; ответы подмены — все тесты на `fakeApi`) и static (`tsc`, `vite build`) | none | Нового теста не нужно: поведение не изменилось. Потерянный вызов или аргумент ломает `tsc`; изменение ответа подмены ломает тесты на ней. Web 9 файлов, 60/60, lint и build зелёные. |

## Removed tests

Удалений нет: ни один тест не потерял риск; тесты изменены только заменой `json(...)` на `Response.json(...)`.

| Test | Reason | In change scope |
|---|---|---|

## Added tests

Level and AC/risk covered are visible in the test file itself (name, path) — not restated here.

Новых тестов нет (web 60 → 60); строки Regression proof entry 7 в силе (`test-plan.entry-07.md`).

| Test | Regression proof |
|---|---|

## Suite run

Written twice per cycle: `impl-test`'s suite gate records an **impacted-set** run here each entry
(`.asd/rules/sprint-lifecycle.md` "Impacted test set"); `impl-review`'s terminal step overwrites
it with the cycle's one **full-suite** run once the last review wave's reviewers are
APPROVE/latched. The `pr` gate always reads whatever is recorded here last — the full-suite
record, by the time `pr` runs. Each per-entry record measures only the tree that entry
analysed, not any tree produced later
(`.asd/rules/sprint-lifecycle.md` "Impacted test set").

- Command: `dotnet test --solution Competency.slnx && npm --prefix web test` (`test` из `commands.yaml`, без изменений)
- Scope: full suite, unscoped — терминальный прогон `impl-review` (`impl-review wave-3/iter-03 suite`) после APPROVE/latched всех трёх волн ревью; перезаписывает запись impacted-прогона entry 8
- Result: pass — backend (`dotnet test --solution Competency.slnx`, xunit v3, общий Testcontainers PostgreSQL `postgres:18.6-trixie`): 102 теста, 102 пройдено, 0 сбоев, 0 пропущено, exit 0, тесты 21 s 361 ms, команда целиком 29 s; web (`npm --prefix web test`, vitest): 9 файлов, 60 тестов пройдено, 0 сбоев, exit 0, 9.32 s, команда целиком 10 s; e2e: не выполнялся (Playwright исключён решением пользователя)
- Lint / build: pass — `npm --prefix web run lint` (biome ci `--error-on-warnings`, 84 файла, и typecheck) exit 0, 3 s; `dotnet build Competency.slnx --tl:off && npm --prefix web run build` (0 предупреждений, 0 ошибок; `tsc`, `vite build`, 3281 модуль) exit 0, 6 s
- HEAD: 376e9d3040f4aeb091823423ccac8580156c6efc — `git status --porcelain` пуст до и после прогона; контейнеры Testcontainers и build-серверы dotnet после прогона не остались

## Defects

Code defects found by the suite. Resolved in `impl` test-fix mode. `Entry` through `Failing test` are never edited once written — the stalemate check compares them (`.asd/rules/sprint-lifecycle.md` "Impl-test phase"). One table only: append rows, never a second `## Defects` section — the check fails on one.

Дефектов продукта в entry 1, 2, 3, 4, 5, 6, 7 и 8 не найдено: новые тесты прошли на HEAD без правок production-кода; в entry 7 красных не было, все красные прогоны — мутации в копии вне репозитория. Красные строки — дефекты тестов, исправленные внутри entry: entry 2 `Ac5_ServerErrorLog_…` (запись журнала доходила до вывода позже ответа, `ApiHost.LogsAfterAsync`); entry 4 `ApiResponse` печатал `Id` и скрывал причину упавшего утверждения (`PrintMembers`). Находка без дефекта: утверждение внешнего ревью wave-2/iter-01 о гонке привязки с увольнением не воспроизводится на коде до исправления (см. строку `bind` в `Risk → check decisions`).

| ID | Entry | Location | Symptom | Failing test | Status | Fix commit |
|---|---|---|---|---|---|---|
