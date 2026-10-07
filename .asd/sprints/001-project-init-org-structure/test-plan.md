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
| 3 |  | delta since entry 2 (133c58c37107b0d4528f9fee2ee765d1e88e5e01) |

## Risk → check decisions

Entry 3 — дельта `133c58c37107b0d4528f9fee2ee765d1e88e5e01...HEAD` без `.asd/**` и `docs/**`: единственный файл `tests/Competency.Tests/PlatformTests.cs` (review-fix bfc19fc, 2 строки), production-код не менялся. Строки entry 2 перенесены в `test-plan.entry-02.md`; заменяющих строк нет.

| Change | Material risk | Chosen check | Decision | Reason |
|---|---|---|---|---|
| `PlatformTests.Ac5_Logs_AreJsonAndCarryNoPasswordsEmailsNamesOrSqlValues`: тест стартует собственный хост вместо `SharedHostAsync` (bfc19fc) | ожидание записи 412 в `LogsAfterAsync` на общем хосте могло удовлетвориться записью чужого теста, и проверка отсутствия значений шла бы по неполному журналу | — | none | Это правка самого теста, нового материального риска нет: проверки и утверждения те же, меняется только изоляция журнала. Новый тест не нужен; изоляцию доказывает сам тест — на своём хосте в журнале только его записи. Идёт в impacted set и зелёный (см. Suite run). |

## Removed tests

Удалений нет.

| Test | Reason | In change scope |
|---|---|---|

## Added tests

Level and AC/risk covered are visible in the test file itself (name, path) — not restated here.

Новых тестов в entry 3 нет (decision `none`).

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
- Scope: full — дельта правит только `PlatformTests.cs` (impacted set: класс `PlatformTests`), но полный набор укладывается в ~20 с, поэтому запущен он; HEAD 8716715
- Result: pass — backend (xunit.v3, Microsoft Testing Platform): итог 87, успешно 87, сбой 0, пропущено 0, 19,3 с, exit 0; web (vitest): 5 файлов, 23 теста пройдено, 0 сбоев, 2,71 с, exit 0; e2e: не выполнялся (Playwright исключён решением пользователя)
- Lint / build: pass — `npm --prefix web run lint` (eslint + typecheck) exit 0; `dotnet build Competency.slnx --tl:off && npm --prefix web run build` exit 0
- HEAD: 8716715 — прогон на дереве, включающем тесты этого entry; commit с `test-plan.md` код не меняет

## Defects

Code defects found by the suite. Resolved in `impl` test-fix mode. `Entry` through `Failing test` are never edited once written — the stalemate check compares them (`.asd/rules/sprint-lifecycle.md` "Impl-test phase"). One table only: append rows, never a second `## Defects` section — the check fails on one.

Дефектов продукта в entry 1, 2 и 3 не найдено: новые тесты прошли на HEAD без правок production-кода. Единственная красная строка entry 2 (`Ac5_ServerErrorLog_…`, запись журнала доходила до вывода позже ответа) — дефект теста, исправлен внутри entry (`ApiHost.LogsAfterAsync`).

| ID | Entry | Location | Symptom | Failing test | Status | Fix commit |
|---|---|---|---|---|---|---|
