---
responsibility:
  owns: project-vetted reference for one technology version (apis used, version specifics, project conventions)
  excludes: adr rationale, code, full stack overview, build commands
  delegates_to: stack.html (overview), commands.yaml (commands); decisions fold per sprint-lifecycle.md "Design-promote phase" fold rule into whichever persistent doc's owns matches
---

# @vitejs/plugin-react @ 6.1.2

## Canonical source
- Official docs/readme: https://github.com/vitejs/vite-plugin-react/tree/main/packages/plugin-react
- Changelog: https://raw.githubusercontent.com/vitejs/vite-plugin-react/main/packages/plugin-react/CHANGELOG.md
- npm: https://www.npmjs.com/package/@vitejs/plugin-react (манифест 6.1.2: `engines.node ^20.19.0 || >=22.12.0`; dependency `@rolldown/pluginutils ^1.0.1`; peer `vite ^8.0.0`; optional peers `oxc-transform-react ^0.152.0`, `@rolldown/plugin-babel ^0.1.7 || ^0.2.0`, `babel-plugin-react-compiler ^1.0.0`)
- Last verified: 2026-10-05 (dist-tag `latest = 6.1.2`; 6.1.2 датирован 2026-10-05 по CHANGELOG — релиз в день проверки)

## API surface used in project
- `import react from '@vitejs/plugin-react'` и `plugins: [react()]` в `vite.config.ts` — без опций.
- Что даёт: React Fast Refresh в dev и JSX-трансформ (в Vite 8 — средствами Oxc; Babel не требуется).
- Не используется: React Compiler (`compiler: true`, `oxc-transform-react`, `babel-plugin-react-compiler`), `@rolldown/plugin-babel` — в стек не входят.

```ts
import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';
export default defineConfig({ plugins: [react()] });
```

## Version-specific notes
- Линия 6.x работает только с Vite 8+ (peer `vite ^8.0.0`); с Vite 7 и ниже несовместима.
- 6.0.0 (2026-03-12): Babel удалён из зависимостей («Vite 8+ can handle React Refresh Transform by Oxc»); автоматическое добавление `react`/`react-dom` в `resolve.dedupe` убрано.
- 6.1.0 (2026-08-19): экспериментальная нативная поддержка React Compiler через `oxc-transform-react` и `compiler: true`; 6.1.1 (2026-08-28): опция `compiler.logDiagnostics`; 6.1.2 (2026-10-05): исправление HMR составных компонентов, `compiler.logDiagnostics` теперь требует `oxc-transform-react >=0.152` и объявлена deprecated в пользу `compiler.reportDiagnostics`, Fast Refresh отключён для не-JSX файлов при включённом компиляторе. Эти опции относятся только к React Compiler (не используется).
- 6.0.x: 6.0.1 расширил peer `@rolldown/plugin-babel` до `^0.2.0`; 6.0.4 — исправлена ошибка `$RefreshSig$ is not defined` при `NODE_ENV=production` в dev; 6.0.5 — устранена регрессия производительности фильтра.

## Deprecations and breaking changes from prior version
5.x → 6.0.0:
- удалены Babel-зависимые возможности («Remove Babel Related Features») — при необходимости Babel подключается отдельно через `@rolldown/plugin-babel` (в проекте не нужен);
- настройка React Compiler перенесена с Babel-конфига на `compiler` (с 6.1);
- поддержка Vite ≤7 прекращена;
- `react`/`react-dom` не добавляются в `resolve.dedupe` автоматически — единственную копию React обеспечивает npm (одна версия 19.3.0 в lock-файле).

## Project conventions
- Плагин один (`react()`), без Babel и без React Compiler (расширение — через Complication Approval).
- Версия закреплена точно; 6.1.2 вышел в день верификации стека — до реализации перепроверить наличие более нового 6.1.x (`latest`) и отсутствие регрессий в changelog.

## Known issues and workarounds
- Не выявлено в версии 6.1.2 (после 6.1.2 в changelog записей нет на момент проверки).
