[REVIEW-design-correctness]: APPROVE

# Review — correctness

- **Phase**: design-review
- **Iteration**: 5

## Findings

| # | Severity | Location | Description | Suggested fix |
|---|---|---|---|---|
| — | — | — | no findings | — |

## Coverage (internal reviewers only)

Порог серьёзности итерации 5 — `critical`: учитываются только критические находки. Просмотрен весь `.diff` (prd.html: AC-34.6, новый AC-34.12; ux-spec.html: U-11, новый макет `m-matrix`, Entry/Exit экранов шага 6, «Доработать», «Согласовать», 412 «перечень изменился», каталог текстов, покрытие AC, «Изменяемые существующие экраны»; adr.html: кто передаёт тег, `MatrixPage` в ADR-6, отклонённая альтернатива, последствия, маршруты). Изменения сверены с кодом.

- `MatrixWorkflow.ActionsOf`. Публикующие переходы (`Submit`, `Approve`, `AcceptChanges`) выдаются только тому, чей сейчас ход. Ссылки на сценарии в `m-matrix` (1–5) повторяют условия показа прежних кнопок. «Вернуть с правками», «Принять и доработать», «Откатить правки» и «Создать новую версию» ничего не публикуют, это совпадает с AC-34.12.
- Других мест в SPA, откуда вызываются `/matrix-versions/{id}/submit|approve|accept-changes`, нет: `transitMatrixVersion` вызывает только `MatrixPage.tsx` (и тест). Поэтому утверждение ADR «других путей SPA к публикующим действиям нет» для версий верно.
- Автор версии всегда руководитель подразделения на момент создания (`MatrixEndpoints.CreateAsync`, `MatrixWorkflow` для новой версии). Значит, мастер (AC-8.1) и «Доработать» (AC-9.1) доступны тем, кто раньше нажимал кнопки на странице. Согласующий обоих шагов покрыт AC-10.1.
- Макет `m-matrix` использует только токены (`var(--ds-xl)`), а у «…» есть `aria-label`.

Несколько наблюдений ниже порога (не учитываются; можно поправить попутно на gate plan):

1. Новый AC-34.12 не упомянут ни в таблице покрытия AC в ux-spec, ни в ADR. Строка покрытия ссылается на AC-34.5…34.7, AC-10.7 и AC-8.10.
2. В `m-matrix` написано, что «Перейти к отправке в мастере» открывает шаг 6 или «шаг, где остановились». По ADR-6 версия с `builderStep = null` (создана вне мастера) открывается на шаге 2, и для неё фраза «где вы остановились» неточна.
3. Если автор перестал быть руководителем подразделения, но ход по-прежнему его (`ActionsOf` проверяет только чей ход), то после переноса отправки в мастер, доступный руководителю (AC-8.1, AC-9.1), он может потерять путь к отправке и «Принять правки». Это редкий крайний случай.

Ledger:

```json
{"manifest_digest":"97a6a33426e3e09f9f623c821ebd7a30e0c56929236847758d7af02434e2d6d0","findings":[],"files":[{"i":".asd/sprints/004-role-desktop-user-journeys/design/prd.html","s":"checked"},{"i":".asd/sprints/004-role-desktop-user-journeys/design/ux-spec.html","s":"checked"},{"i":".asd/sprints/004-role-desktop-user-journeys/design/adr.html","s":"checked"}],"rules":[{"i":"Draft correctness [design-review]","s":"pass"},{"i":"Bugs [impl-review]","s":"n/a","p":"outside phase gate"},{"i":"Security [impl-review]","s":"n/a","p":"outside phase gate"},{"i":"Contracts [impl-review]","s":"n/a","p":"outside phase gate"},{"i":"Best practices [impl-review]","s":"n/a","p":"outside phase gate"},{"i":"AC coverage trace [impl-review]","s":"n/a","p":"outside phase gate"},{"i":"UI conformance [design-review — `n/a: outside phase gate` without a ux-spec/design-system draft; impl-review — conditional on a UI surface in scope]","s":"pass"},{"i":".asd/project/custom-common-rules.md","s":"pass"},{"i":".asd/project/custom-design-rules.md","s":"pass"}],"sections":[{"i":"Draft correctness [design-review]","s":"reviewed"},{"i":"Bugs [impl-review]","s":"n/a","p":"outside phase gate"},{"i":"Security [impl-review]","s":"n/a","p":"outside phase gate"},{"i":"Contracts [impl-review]","s":"n/a","p":"outside phase gate"},{"i":"Best practices [impl-review]","s":"n/a","p":"outside phase gate"},{"i":"AC coverage trace [impl-review]","s":"n/a","p":"outside phase gate"},{"i":"UI conformance [design-review — `n/a: outside phase gate` without a ux-spec/design-system draft; impl-review — conditional on a UI surface in scope]","s":"reviewed"}]}
```

## Verdict
APPROVE

## Next action
Ревьюер завершил работу; со стороны correctness черновики могут перейти к gate plan.
