---
responsibility:
  owns: cross-sprint dispositions of retro rows
  excludes: retro content (retrospective.html), sprint decisions and HEAD verification evidence (decisions-log.md)
  delegates_to: <sprint>/retrospective.html (row content, home), <sprint>/decisions-log.md (sprint decisions, verification evidence)
---

# Retro backlog

<!--
Intake, ownership and lifecycle: .asd/rules/sprint-lifecycle.md "Retro intake" — not restated here.
One line per retro row, updated in place. Row: <NNN-slug>#A-N | <NNN-slug>#P-N. Row, Acts on and Disposition are English literals.
Disposition: deferred | included | rejected | closed (closed = verified already resolved at HEAD).
Decided in: the sprint that last decided the row. Guardrail: the row's text; escape | as \|.
-->

| Row | Acts on | Disposition | Decided in | Guardrail |
|---|---|---|---|---|
| 001-project-init-org-structure#P-1 | consumer | included | 002-mail-invite-password-reset | Заменить раздел «Verification depth in impl»: отдельного полного цикла ручных проверок перед impl-test нет — проверку ведёт impl-test автотестами в репозитории (интеграционные тесты API на настоящей PostgreSQL через существующий хост тестов, тесты web на vitest с подменой API); impl остаётся на минимальных проверках; замер производительности — только для Task с объявленным perf-риском, на переиспользуемом скрипте наполнения 10 000+ сотрудников. Playwright и e2e не используются (решение пользователя). |
| 001-project-init-org-structure#P-2 | consumer | rejected | 004-role-desktop-user-journeys | Ограничивать спринт одной подсистемой; для спринта с заметным UI фиксировать направление интерфейса до impl (макеты или workflow с UX-черновиком), а не правками на гейте impl assessment. |
| 001-project-init-org-structure#P-3 | consumer | included | 002-mail-invite-password-reset | Каждое правило, охраняющее инвариант через несколько строк (последний активный администратор, счётчик неверных паролей, одна учётная запись на сотрудника, каскад увольнения), объявляет в plan свою блокировку и её место в общем порядке блокировок и получает детерминированный тест гонки в impl-test. |
| 001-project-init-org-structure#P-4 | consumer | included | 002-mail-invite-password-reset | Агенты не выполняют разрушающие команды Docker (`down -v`, `volume rm`, `volume prune`) для dev-стека пользователя и томов с фиксированным именем; временные прогоны используют только собственные именованные тома и удаляют их по точному имени. |
| 001-project-init-org-structure#P-5 | consumer | included | 002-mail-invite-password-reset | Вспомогательные типы, общие для модулей (ответ постраничного списка, отказы с ProblemDetails, экранирование LIKE, пределы страниц), живут в `Competency.Platform`, а не копируются в каждый модуль. |
| 002-mail-invite-password-reset#A-2 | consumer | included | 004-role-desktop-user-journeys | Агенты меняют существующие файлы своей памяти точечной правкой и никогда не перезаписывают файл памяти целиком. |
| 002-mail-invite-password-reset#P-1 | consumer | rejected | 004-role-desktop-user-journeys | Для спринта с видимыми пользователю текстами (письма, подсказки, заголовки, метки) plan перечисляет эти тексты и элементы бренда списком на утверждение пользователем до impl, а не после первой smoke-проверки. |
| 002-mail-invite-password-reset#P-2 | consumer | included | 003-positions-career-competency | Dev и tester перед коммитом проверяют свой дифф поиском комментариев в теле функций и ссылок на AC и спринт в комментариях (`code-style.md` §7, §8) и исправляют их до ревью. |
| 003-positions-career-competency#A-4 | consumer | included | 004-role-desktop-user-journeys | Рецепты scratch-БД и запуска API в памяти dev пометить «только impl-test / tester» или удалить; в impl проверка ограничена сборкой, линтером и контрактом. |
| 003-positions-career-competency#A-6 | consumer | included | 004-role-desktop-user-journeys | Перед коммитом UI сверять каждый изменённый экран с таблицей компонентов DESIGN.md: не больше двух видимых действий строки, свой текст пустого состояния, токены всплывающих меню темы. |
| 003-positions-career-competency#P-1 | consumer | rejected | 004-role-desktop-user-journeys | В спринте с новыми экранами на gate plan показывать макет каждого экрана (столбцы, действия строк, фильтры, окна, тексты) и собирать замечания до impl. |
| 003-positions-career-competency#P-2 | consumer | included | 004-role-desktop-user-journeys | Каждый изменяющий UI-поток блокирует правку на время сохранения, не даёт переходов при несохранённых правках, а после 412 перечитывает запись и повторяет с новой версией. |
