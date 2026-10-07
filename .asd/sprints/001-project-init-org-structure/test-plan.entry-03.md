# Test plan — sprint 001-project-init-org-structure — entry 3 (rotated segment)

Строки `Risk → check decisions`, `Removed tests` и `Added tests` entry 3 (`HEAD analysed` 3ae1630d0671e0ae021b7308610c13fb5b367f8d, scope «delta since entry 2»), перенесённые сюда на strategy pass entry 4 (`artifact-layout.md` "Test plan" Rotation). Сегмент не правится: строка, чей риск изменился, заменяется строкой в `test-plan.md`. Review-fix строк тестировщика с прошлого entry нет: review-fix wave-2/iter-01 правку тестов (a1f3f47) сделал без строк в плане.

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
