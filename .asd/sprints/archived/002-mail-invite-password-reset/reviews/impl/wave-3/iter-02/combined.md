[REVIEW-impl-combined]: CONCERNS

# Review — combined

- **Phase**: impl-review
- **Iteration**: wave-3/iter-02

## Findings

| # | Severity | Location | Description | Suggested fix |
|---|---|---|---|---|
| F1 | high | web/src/features/users/userMail.test.tsx:208 (AC-6, AC-11) | Новый тест «shows progress and issues one request only while a send is pending…» содержит комментарий внутри тела функции `it(...)`: «// antd keeps a destroyed message in the DOM for its leave animation, which jsdom never finishes». `code-style.md` §7 запрещает комментарии в теле функции всегда, кроме маркера `TODO(sprint-…)`; это единственный такой комментарий во всём `web/src` (Grep `^\s+// `). Severity по записи «In-code doc comments» — high. | Убрать комментарий и перенести смысл в имя или JSDoc: например, вспомогательная функция в файле теста `/** antd keeps a closed message in the DOM for its leave animation, which jsdom never finishes. */ function isClosing(text: string)`, которая возвращает `screen.queryByText(text)?.closest("[role=alert]")`, а в тесте `await waitFor(() => expect(isClosing("Отправка письма…")).toHaveClass("ant-message-fade-leave"))`. |
| F2 | medium | web/src/features/users/useSendUserMail.ts:28-36 (AC-6, AC-9) | JSDoc хука `useSendUserMail` теперь стоит над новой константой `const SENDING_KEY = "send-user-mail";` (строка 34), а не над экспортируемой функцией (строка 36). JSDoc относится к следующему объявлению, поэтому описание получила неэкспортируемая константа, а у экспортируемого `useSendUserMail` описания нет. Это нарушает §7 («doc comments mandatory on every public/exported … member», «update doc comments when code changes»). Сам текст тоже устарел. «Returns the action» неверно: хук теперь возвращает `{ sendMail, isSending }`. «the row being sent cannot be sent again» тоже неверно: по решению пользователя от 2026-10-08 пока идёт любая отправка, пункт отключён во всех строках. | Поставить `const SENDING_KEY` над JSDoc (или перенести его ниже функции) так, чтобы комментарий стоял прямо над `export function useSendUserMail()`. Поправить текст: хук возвращает действие отправки и признак того, что отправка идёт; пока она идёт, повторная отправка недоступна ни для одной учётной записи (повторный клик ушёл бы с устаревшей версией и получил бы 412). |

## Coverage (internal reviewers only)

Проверен дифф `bd06a305e29f7920.diff` (6 файлов) и исправления по iter-01 из `decisions-log.md` (review-fix wave-3/iter-01, 8942059). Решения пользователя от 2026-10-08 приняты как данность: пункт меню отключён во всех строках, пока идёт любая отправка; текст «Отправка письма…»; общее предупреждение без «Учётная запись сохранена».

Все находки iter-01 закрыты:
- F1 / external #1: в `sendMail` есть проверка `send.isPending`, в `onMutate` показывается `message.loading` с ключом и `duration: 0`, в `onSettled` вызывается `message.destroy`, пункт меню получает `disabled: isSending`. TanStack v5 ждёт промис, который возвращает `onSettled`, поэтому `isPending` держится до конца перечитывания списка, и следующий клик уходит уже с новой версией.
- F2: `Intl.PluralRules("ru")`. После «короче» нужен родительный падеж, поэтому «one» даёт «символа», а few/many дают «символов» («не короче 2/5/11 символов», «не короче 21 символа»). Логика верна.
- F3: текст предупреждения подходит всем трём вызовам `useMailReport` (`UserModal`, `useSendUserMail`).

Тесты: `deferred` есть в `web/src/test/fakeApi.ts:55`. Fail-first для обоих новых тестов записан в `test-plan.md` (строки 47–48). Ручные проверки `test-plan.md` не изменились: результаты есть во всех строках, fail в AC-18 закрыт повтором. Находки по ним нет.

Над новым кодом есть только Documentation-строки, вынесенные выше: ссылок на документы и AC в коде нет, `TODO(sprint` нет.

Память `feedback_frontend-wave-probes.md`: утверждения про `Smtp:Timeout` 15 с, `describeApiError` («Данные изменены другим пользователем…», `apiErrors.ts:14`) и правило вычисления строк по диффу совпадают с кодом и с форматом unified diff.

```json
{"manifest_digest":"fdf2d79cf46a3edaafed1f15dc4df04cec69a3d24fc8471f77132be5d01feba0","findings":["F1","F2"],"files":[{"i":".claude/agent-memory/asd-reviewer-combined/feedback_frontend-wave-probes.md","s":"checked"},{"i":"web/src/features/auth/linkScreens.test.tsx","s":"checked"},{"i":"web/src/features/auth/passwordPolicy.ts","s":"checked"},{"i":"web/src/features/users/UserListPage.tsx","s":"checked"},{"i":"web/src/features/users/useSendUserMail.ts","s":"checked"},{"i":"web/src/features/users/userMail.test.tsx","s":"checked"}],"rules":[{"i":"Draft correctness [design-review]","s":"n/a","p":"outside phase gate"},{"i":"Bugs [impl-review]","s":"pass"},{"i":"Security [impl-review]","s":"pass"},{"i":"Contracts [impl-review]","s":"pass"},{"i":"Best practices [impl-review]","s":"finding","f":"F2"},{"i":"AC coverage trace [impl-review]","s":"pass"},{"i":"UI conformance [design-review — `n/a: outside phase gate` without a ux-spec/design-system draft; impl-review — conditional on a UI surface in scope]","s":"pass"},{"i":"Over-engineering checklist [design-review, impl-review] — critical, undroppable","s":"pass"},{"i":"Structure / cohesion checklist [design-review, impl-review] — critical, undroppable","s":"pass"},{"i":"Complexity-vs-value tradeoff [design-review, impl-review]","s":"pass"},{"i":"Perf budget compliance [impl-review]","s":"n/a","p":"no budgets defined"},{"i":"Perf anti-patterns [impl-review]","s":"pass"},{"i":"Algorithmic complexity [impl-review]","s":"pass"},{"i":"Regression detection [impl-review]","s":"pass"},{"i":"Hot path identification [impl-review]","s":"pass"},{"i":"SSoT","s":"pass"},{"i":"Template adherence","s":"n/a","p":"no templated artefact in scope"},{"i":"HTML shell wrapping","s":"n/a","p":"no HTML file in scope"},{"i":"Provenance","s":"n/a","p":"no HTML file in scope"},{"i":"Traceability","s":"n/a","p":"no HTML file in scope"},{"i":"Persistent actuality (impl-review)","s":"pass"},{"i":"In-code doc comments (impl-review, `code-style.md` §7)","s":"finding","f":"F1"},{"i":"Stub-resolution verification (impl-review)","s":"pass"},{"i":"Framework mode (`self_hosting: enabled`, impl-review only)","s":"n/a","p":"self_hosting not enabled"},{"i":"Documentation economy","s":"pass"},{"i":"Custom rules consistency","s":"pass"},{"i":"Overall quality","s":"pass"},{"i":".asd/project/custom-common-rules.md","s":"pass"},{"i":".asd/project/custom-coding-rules.md","s":"pass"}],"sections":[{"i":"Draft correctness [design-review]","s":"n/a","p":"outside phase gate"},{"i":"Bugs [impl-review]","s":"reviewed"},{"i":"Security [impl-review]","s":"reviewed"},{"i":"Contracts [impl-review]","s":"reviewed"},{"i":"Best practices [impl-review]","s":"reviewed"},{"i":"AC coverage trace [impl-review]","s":"reviewed"},{"i":"UI conformance [design-review — `n/a: outside phase gate` without a ux-spec/design-system draft; impl-review — conditional on a UI surface in scope]","s":"reviewed"},{"i":"Over-engineering checklist [design-review, impl-review] — critical, undroppable","s":"reviewed"},{"i":"Structure / cohesion checklist [design-review, impl-review] — critical, undroppable","s":"reviewed"},{"i":"Complexity-vs-value tradeoff [design-review, impl-review]","s":"reviewed"},{"i":"Perf budget compliance [impl-review]","s":"n/a","p":"no budgets defined"},{"i":"Perf anti-patterns [impl-review]","s":"reviewed"},{"i":"Algorithmic complexity [impl-review]","s":"reviewed"},{"i":"Regression detection [impl-review]","s":"reviewed"},{"i":"Hot path identification [impl-review]","s":"reviewed"},{"i":"Overall quality","s":"reviewed"}]}
```

Over-engineering / structure: всё проверенное отнесено к `keep-as-is`.
- Проверка `if (!send.isPending)` в `sendMail` почти дублирует `disabled: isSending`, но это одно условие: оно защищает контракт хука при вызове не из меню.
- Константа `SENDING_KEY` нужна, потому что ключ используется в двух местах (`loading`, `destroy`).
- `const PLURAL` на уровне модуля создаёт `Intl.PluralRules` один раз, а не при каждом рендере.

## Verdict
CONCERNS: 2

## Next action
В review-fix создатель исправляет две находки:
- F1: убрать комментарий из тела теста `userMail.test.tsx`, перенести смысл в имя или JSDoc вспомогательной функции.
- F2: вернуть JSDoc прямо над `export function useSendUserMail()` и обновить его текст: возвращаемое значение `{ sendMail, isSending }`, отключение во всех строках.

После исправлений нужна итерация wave-3/iter-03.

## Escalations (optional)
