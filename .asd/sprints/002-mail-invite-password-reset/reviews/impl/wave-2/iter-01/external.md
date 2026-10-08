[REVIEW-impl-external]: CONCERNS

# External Review Report

- **Phase**: impl-review
- **Iteration**: wave-2/iter-01
- **Severity floor (this iter)**: low
- **Unreviewed files**: none

## Kept findings

| # | Severity | Location | Description | Suggested fix |
|---|---|---|---|---|
| 1 | high | tests/Competency.Tests/LastAdministratorTests.cs:246-251 (Codex F1, указал :240) | Сценарий `Ac7_TwoConcurrentRemovalsOfTheOnlyTwoAdministrators_LeaveExactlyOne` теперь включает приглашённого администратора (`InviteUserAsync`, строка 240), но не является детерминированным тестом гонки по AC-14: общий `TaskCompletionSource` и 4 повтора лишь запускают оба запроса одновременно, не обеспечивая перекрытия транзакций; последовательный прогон тоже проходит все проверки, поэтому регрессия блокировки может остаться незамеченной. Расположение перенесено на подготовку гонки, строки 246-251. | Удерживать первое снятие внутри его транзакции после проверки инварианта, затем запустить второе и убедиться, что оно ждёт общую блокировку; отпустить первое только после этого. Сохранить проверку, что остаётся ровно один зарегистрированный администратор. |

Codex оценил находку как «major» (→ high). Привязка к решениям plan.md о способе построения теста гонки не проверялась.

## Dropped findings (counts only)

- Below severity floor (iter 1, floor low): 0
- Nitpick, by category: none

## Verdict
CONCERNS: 1

## Next action
Создатель решает по находке 1: сделать тест гонки детерминированным, как предложено, или записать в decisions-log принятие подхода с повторами. Затем iter-02 (порог medium и выше).
