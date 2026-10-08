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
| 001-project-init-org-structure#P-2 | consumer | deferred | 002-mail-invite-password-reset | Ограничивать спринт одной подсистемой; для спринта с заметным UI фиксировать направление интерфейса до impl (макеты или workflow с UX-черновиком), а не правками на гейте impl assessment. |
| 001-project-init-org-structure#P-3 | consumer | included | 002-mail-invite-password-reset | Каждое правило, охраняющее инвариант через несколько строк (последний активный администратор, счётчик неверных паролей, одна учётная запись на сотрудника, каскад увольнения), объявляет в plan свою блокировку и её место в общем порядке блокировок и получает детерминированный тест гонки в impl-test. |
| 001-project-init-org-structure#P-4 | consumer | included | 002-mail-invite-password-reset | Агенты не выполняют разрушающие команды Docker (`down -v`, `volume rm`, `volume prune`) для dev-стека пользователя и томов с фиксированным именем; временные прогоны используют только собственные именованные тома и удаляют их по точному имени. |
| 001-project-init-org-structure#P-5 | consumer | included | 002-mail-invite-password-reset | Вспомогательные типы, общие для модулей (ответ постраничного списка, отказы с ProblemDetails, экранирование LIKE, пределы страниц), живут в `Competency.Platform`, а не копируются в каждый модуль. |
