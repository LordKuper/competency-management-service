---
responsibility:
  owns: project-vetted reference for one technology version (apis used, version specifics, project conventions)
  excludes: adr rationale, code, full stack overview, build commands
  delegates_to: stack.html (overview), commands.yaml (commands); decisions fold per sprint-lifecycle.md "Design-promote phase" fold rule into whichever persistent doc's owns matches
---

# Microsoft.VisualStudio.JavaScript.Sdk @ 1.0.5984942

## Canonical source
- NuGet: https://www.nuget.org/packages/Microsoft.VisualStudio.JavaScript.SDK/1.0.5984942 ; nuspec: https://api.nuget.org/v3-flatcontainer/microsoft.visualstudio.javascript.sdk/1.0.5984942/microsoft.visualstudio.javascript.sdk.nuspec — id `Microsoft.VisualStudio.JavaScript.SDK`, авторы Microsoft, `packageTypes`: `MSBuildSdk`, зависимостей нет, лицензия — файл `LICENSE.txt` в пакете, `projectUrl` https://aka.ms/jsps
- Обзор: https://learn.microsoft.com/en-us/visualstudio/javascript/javascript-in-visual-studio ; свойства MSBuild: https://learn.microsoft.com/en-us/visualstudio/javascript/javascript-project-system-msbuild-reference
- Last verified: 2026-10-07 (лицензия — по странице License на nuget.org, пересказанной инструментом чтения: текст пакета `LICENSE.txt` не разбирался)

## API surface used in project
- Проект `web/web.esproj` (JavaScript Project System, JSPS) с `Sdk="Microsoft.VisualStudio.JavaScript.Sdk/1.0.5984942"` — версия закреплена в атрибуте `Sdk`.
- Свойства: `StartupCommand` = `npm run dev` (запуск Vite по F5), `ShouldRunNpmInstall` = `false` и `ShouldRunBuildScript` = `false` (по умолчанию оба `true`: `npm install` на Restore/Build и `npm run build` на Build). Пример:

```xml
<Project Sdk="Microsoft.VisualStudio.JavaScript.Sdk/1.0.5984942">
  <PropertyGroup>
    <StartupCommand>npm run dev</StartupCommand>
    <ShouldRunNpmInstall>false</ShouldRunNpmInstall>
    <ShouldRunBuildScript>false</ShouldRunBuildScript>
  </PropertyGroup>
</Project>
```

## Version-specific notes
- JSPS (`.esproj`) — проектный тип Visual Studio 2022 и новее; для Visual Studio 2022 упрощённый шаблон появился в 17.5. Поддержку JavaScript/TypeScript даёт компонент Visual Studio «JavaScript and TypeScript» (в `deploy/dev/README.md` — «Поддержка JavaScript и TypeScript» в нагрузке «ASP.NET и разработка веб-приложений»).
- Это MSBuild SDK, а не рантайм-библиотека: в `package.json`, образ, `dotnet publish` и бандл не попадает.
- Лицензия пакета — Microsoft Software License Terms для дополнений и расширений Visual Studio (проприетарная, вне допустимого списка лицензий `stack.html`).

## Deprecations and breaking changes from prior version
- Не выявлено в проверенных страницах; версия закреплена точно и меняется только через спринт (новые сборки SDK выходят часто — 186 версий в индексе NuGet на 2026-10-07).

## Project conventions
- Только локальная разработка в Visual Studio: исключение из «минимум зависимостей» и из списка лицензий принято пользователем 2026-10-06 (`stack.html`, «Ограничения»); CI, Docker-образ и тесты проект не используют.
- `npm install` из MSBuild отключён: зависимости ставятся вручную (`npm ci` в `web/`), чтобы установка не расходилась с `package-lock.json`.
- Проект входит в `Competency.slnx` (`<Build />`, `<Deploy />`), но `dotnet build Competency.slnx` и `dotnet test` его не ломают: `npm install` и `npm run build` не запускаются (проверено в Task 11, `deploy/dev/README.md`).

## Known issues and workarounds
- Не проверялось: поведение `.esproj` вне Visual Studio и при `dotnet build` на машине без компонентов JavaScript Visual Studio (в CI и в образе проект не участвует).
