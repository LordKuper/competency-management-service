---
responsibility:
  owns: cross-sprint dispositions of retro rows
  excludes: retro content (retrospective.html), sprint decisions and HEAD verification evidence (decisions-log.md)
  delegates_to: <sprint>/retrospective.html (row content, home), <sprint>/decisions-log.md (sprint decisions, verification evidence)
---

# Retro backlog

<!--
Intake, ownership and lifecycle: .asd/workflows/asd-phase-scope.md step 2a — not restated here.
One line per retro row, updated in place. Row: <NNN-slug>#A-N | <NNN-slug>#P-N. Row, Acts on and Disposition are English literals.
Disposition: deferred | included | rejected | closed. The first three are the user's explicit choice per row, never a default. closed = verified already resolved at HEAD.
Decided in: the sprint that last decided the row. Guardrail: the row's text; escape | as \|.
-->

| Row | Acts on | Disposition | Decided in | Guardrail |
|---|---|---|---|---|
| 004-role-desktop-user-journeys#A-4 | consumer | rejected | 005-org-structure-excel-import | Агенты не выполняют git stash, taskkill, перемещение файлов вне репозитория и scratchpad и рекурсивное удаление через junction или symlink; связку снимают только удалением самой ссылки |
| 004-role-desktop-user-journeys#A-7 | consumer | rejected | 005-org-structure-excel-import | Тесты находят корень репозитория от рабочего каталога или git, а не от каталога сборки; агентские dotnet build/test идут с --artifacts-path вне src/*/bin, пока у пользователя запущены Api или IDE |
| 004-role-desktop-user-journeys#P-1 | consumer | included | 005-org-structure-excel-import | Исправляя нарушение «Mutating UI flows» в одной форме, dev в том же раунде проверяет и исправляет все формы с тем же приёмом (Form disabled, явный disabled=false, перечитывание после 412) |
| 004-role-desktop-user-journeys#P-2 | consumer | included | 005-org-structure-excel-import | Задача, удаляющая поверхность (категории, старые миграции, поля контракта), сама приводит тесты к компиляции; отдельная задача test-compile не планируется |
| 005-org-structure-excel-import#P-1 | consumer | rejected | 006-promotion-waves | Разбор загружаемого файла ограничивает каждое измерение, от которого растут память и время (размер и записи архива, строки, глубина вложенности, длина строк), а impl-test проверяет каждый предел маленьким враждебным файлом |
| 005-org-structure-excel-import#P-3 | consumer | included | 006-promotion-waves | Надписи интерфейса сообщают зрителю только то, что нужно ему для действия: без серверных правил, условий, упоминаний журнала и сведений для других ролей |
| 005-org-structure-excel-import#P-4 | consumer | included | 006-promotion-waves | Расширяя роли доступ к данным, план перечисляет каждую ссылку и маршрут, куда ведут новые видимые пункты, и проверяет охрану маршрута для этой роли |
| 006-promotion-waves#A-1 | consumer | included | 007-rename-competency-to-kalibr | Считать порог теста, закреплённый в `docs/architecture/` (бюджет SQL-команд, лимиты), контрактом: красный прогон против него — дефект кода `D-N`, порог не менять |
| 006-promotion-waves#A-5 | consumer | included | 007-rename-competency-to-kalibr | Открывать операцию `suite` перед каждым прогоном набора и закрывать её сразу после прогона, включая прогон до написания тестов |
| 006-promotion-waves#P-1 | consumer | included | 007-rename-competency-to-kalibr | Для спринта с новыми экранами нескольких ролей согласовывать на scope, до плана, макеты экранов каждой роли и сквозной проход процесса по этапам |
