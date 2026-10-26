---
responsibility:
  owns: test approach for sprint change scope, removal reasons, no-test decisions, suite run result, code defects found by tests, manual-verification spec (single home — never duplicated in a review file)
  excludes: task breakdown, requirements, review verdicts, code, change surface (derivable from the diff)
  delegates_to: plan.md (tasks), persistent docs (requirements), reviews/impl/wave-<K>/iter-NN/testing.md (verdict)
---

# Test plan — sprint 004-role-desktop-user-journeys, entry 9 (rotated)

Строки записи 9 перенесены из `test-plan.md` при входе записи 10 (`artifact-layout.md` «Test plan»); сегмент не правится.

## Risk → check decisions

Дельта записи 9 — `git diff a1b6ed8...HEAD`: исправление `D-3` (`8d9f3a5`), удаление не вызываемого `approveDraft` (`b7db7b5`), отметка `D-3` (`db1bf1c`).

| Change | Material risk | Chosen check | Decision | Reason |
|---|---|---|---|---|
| `BlockModal`: `CompetencyPicker` получает `isSaving`, `getTitleCheckboxProps` запирает «Select all» на время сохранения (`8d9f3a5`) | change: workflow state machine — «Select all» менял состав блока, пока шло сохранение (AC-61) | уже написанный тест `BlockModal.test.tsx` › `holds the name, the unit and the composition while the save is in flight, and frees them when it is refused`; красный на `a1b6ed8` (`D-3`), зелёный теперь | none | Регрессионный тест записи 8 покрывает риск, нового не нужно; fail-first — красный прогон записи 8 (строка `D-3`) |
| `catalogApi.ts`: удалён `approveDraft` (`b7db7b5`) | нет поведения: функция без вызывающих, тест удалён в записи 8 | `tsc` (`lint`, `build`) ловит оставшиеся ссылки; `grep approveDraft web/src` — пусто | none | Исчезнувший экспорт проверяет компилятор |

## Removed tests

Нет.

## Added tests

Level and AC/risk covered are visible in the test file itself (name, path) — not restated here.

| Test | Regression proof |
|---|---|
| — | нет новых тестов |
