# ASP.NET Core Runtime — образ `mcr.microsoft.com/dotnet/aspnet` @ 10.0.12 (noble)

Область: базовый образ итогового контейнера `app` (framework-dependent publish поверх готового runtime-образа). Сам фреймворк (Minimal API, Identity, EF Core) — `aspnetcore-10.0.12.md`; веб-сервер — `kestrel-10.0.12.md`; сборка образов — `docker-multi-stage-dockerfile-1.md`.

## Canonical source
- Official docs (образ): https://github.com/dotnet/dotnet-docker/blob/main/README.aspnet.md ; Dockerfile: https://github.com/dotnet/dotnet-docker/blob/main/src/aspnet/10.0/noble/amd64/Dockerfile , https://github.com/dotnet/dotnet-docker/blob/main/src/runtime-deps/10.0/noble/amd64/Dockerfile
- Release 10.0.12 (2026-09-08): https://github.com/dotnet/core/blob/main/release-notes/10.0/10.0.12/10.0.12.md ; метаданные канала: https://builds.dotnet.microsoft.com/dotnet/release-metadata/10.0/releases.json (LTS, EOL 2028-11-14)
- Breaking changes (контейнеры): https://learn.microsoft.com/en-us/dotnet/core/compatibility/containers/10.0/default-images-use-ubuntu ; SIGTERM: https://learn.microsoft.com/en-us/dotnet/core/compatibility/core-libraries/10.0/sigterm-signal-handler
- Хостинг в Docker (Microsoft): https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/docker/building-net-docker-images?view=aspnetcore-10.0
- Last verified: 2026-10-06 (раздел «Проверено на Docker» — запуском, MS-1; остальное — 2026-10-05, по документации)

## API surface used in project
- Образ: `mcr.microsoft.com/dotnet/aspnet:10.0.12-noble` (общий тег; платформенный — `10.0.12-noble-amd64`); в манифестах закрепляется по digest; целевая платформа — linux/amd64.
- Слои: `runtime-deps` (Ubuntu 24.04 `noble`; `ca-certificates`, `libc6`, `libgcc-s1`, `libicu74`, `libssl3t64`, `libstdc++6`, `tzdata`, `tzdata-legacy`) → .NET runtime → `Microsoft.AspNetCore.App` 10.0.12 (`ENV ASPNET_VERSION=10.0.12`, `/usr/share/dotnet`).
- Переменные образа: `ASPNETCORE_HTTP_PORTS=8080` (приложение слушает 8080), `APP_UID=1654`, `DOTNET_RUNNING_IN_CONTAINER=true`. Пользователь `app` (uid/gid 1654) создан, но **`USER` в образе не задан** — включается в Dockerfile приложения: `USER $APP_UID`.
- Итоговая стадия `app` (framework-dependent): example:
  `FROM mcr.microsoft.com/dotnet/aspnet:10.0.12-noble@sha256:<digest>` → `USER $APP_UID` → `COPY --from=publish /app ./` → `ENTRYPOINT ["dotnet", "<App>.dll"]`.
- Kubernetes (workload `app`, один экземпляр на старте): порт контейнера 8080; `securityContext.runAsUser: 1654` (числовой UID из образа), `runAsNonRoot`; пробы readiness/liveness/startup — на health-эндпойнты приложения (в образе `HEALTHCHECK` нет); конфигурация — ConfigMap, секреты — Secret (env или смонтированные файлы).

## Version-specific notes
- Риск знаний (Phase 5): **MEDIUM** — мажор .NET 10 (LTS) вышел до среза знаний, но патч 10.0.12 и поведение образов 10 (Ubuntu по умолчанию) новее; release notes, breaking changes и Dockerfile'ы прочитаны.
- 10.0.12 (2026-09-08) — security-релиз: 6 CVE (CVE-2026-69439, CVE-2026-71328, CVE-2026-69522, CVE-2026-69304, CVE-2026-58649, CVE-2026-69806); runtime и ASP.NET Core = 10.0.12; SDK-пара — 10.0.401 и 10.0.112; 10.0.11 вышел 2026-08-11 — ежемесячный цикл патчей (stack.html).
- Образ содержит ICU 74 и tzdata — полная глобализация (русская культура/`ru-RU`) без `InvariantGlobalization`; chiseled-варианты (distroless) и Alpine ICU/tzdata по умолчанию не содержат, shell и пакетного менеджера в chiseled нет — поэтому выбран полноценный `noble`.
- В образе **нет шрифтов** (не устанавливаются на уровне `runtime-deps`): кириллические шрифты для PDF/XLSX/UI поставляются внутри образа `app` (stack.html; `IFontResolver` для PDFsharp-MigraDoc).
- Приложения в официальных образах слушают порт 8080 (с .NET 8) — привилегированные порты не нужны, что совместимо с non-root.
- Framework-dependent publish: патчи runtime приходят с базовым образом (пересборка вне контура и доставка по регламенту stack.html), runtime pack не нужен.

## Deprecations and breaking changes from prior version
- .NET 9 → 10: теги по умолчанию (`10.0`, `…:10.0-noble`) — **Ubuntu 24.04**; Debian-образы для .NET 10 не выпускаются (раньше теги по умолчанию были Debian). Проверять приложение на Ubuntu-образах.
- .NET 10: runtime по умолчанию не ставит обработчик SIGTERM (ОС завершает процесс сразу; события `AppDomain.ProcessExit` / `AssemblyLoadContext.Unloading` не вызываются). Для ASP.NET-приложений действий не требуется — хост (`UseConsoleLifetime`) регистрирует SIGTERM и обеспечивает graceful shutdown; код вне хоста на `ProcessExit` не рассчитывать.
- Остальные изменения .NET 10 (cookie-аутентификация для API-эндпойнтов, `WebHostBuilder` obsolete и др.) — `aspnetcore-10.0.12.md`, `kestrel-10.0.12.md`, `csharp-14.md`.

## Project conventions
- Образ `app` собирается на машине с интернетом вне контура и доставляется готовым (registry либо tar-импорт на узлы, Q11); перед переносом — сканирование и SBOM; версии меняются только через спринт.
- Базовый образ закрепляется по digest; тег `10.0.12-noble` плавающий (пересборка слоёв ОС без смены версии .NET). На сборочном хосте не-amd64 платформу указывать явно (`--platform linux/amd64`).
- Запуск — только не от root (`USER $APP_UID`, `runAsUser: 1654`); том/каталоги, куда пишет приложение (например ключи DataProtection, вложения), доступны этому uid — `fsGroup`/владелец томов в design.
- Секреты и конфигурация — не в образе, не в репозитории (stack.html).
- Версия runtime образа `app` = версии SDK-пары в build-стадии (`dotnet-sdk-image-10.0.401.md`: SDK 10.0.401 ↔ runtime 10.0.12).

## Known issues and workarounds
- `System.IO.IOException: The configured user limit (128) on the number of inotify instances has been reached` (Microsoft, хостинг в Docker): уменьшить число наблюдателей конфигурации — `DOTNET_HOSTBUILDER__RELOADCONFIGONCHANGE=false`.
- Порядок запуска `app`/`db` Kubernetes не гарантирует: `app` ждёт готовности БД (повторы/проба) — детали в design.
- Debian-образ .NET 10 невозможен без собственной сборки образа (Microsoft: «may need to create and maintain custom container images») — стек принимает Ubuntu `noble`.

## Проверено на Docker (Task 10, 2026-10-06, MS-1)
- Образ `mcr.microsoft.com/dotnet/aspnet:10.0.12-noble`: digest на 2026-10-06 — `sha256:222759b391a1aaf241166672c8f99b2d4ada452e7b5319f3c6e8f265a37b5ad4` (manifest list, linux/amd64); Ubuntu 24.04.5 LTS, `Microsoft.NETCore.App` и `Microsoft.AspNetCore.App` 10.0.12; пользователь `app` (1654:1654, `/home/app`, `/bin/sh`); в образе есть `/bin/sh` и `tzdata`, нет `curl`/`wget` (пробы — `httpGet`, как в манифестах); `libicu*` присутствует (12 файлов), `libgssapi*` нет.
- Итоговый образ `app` под uid 1654 с `--cap-drop ALL --security-opt no-new-privileges` стартует, слушает 8080, пишет ключи DataProtection в `/var/lib/competency/keys` (том наследует владельца 1654 из образа; в Kubernetes — `fsGroup` манифеста, не проверялось), останавливается по SIGTERM за 1 с с кодом 0 (хост обработал сигнал, в логе «Application is shutting down...»). RSS после прогона нагрузочной проверки — около 138 МиБ.
- Каждый старт печатает в stderr `Cannot load library libgssapi_krb5.so.2` / `Error: libgssapi_krb5.so.2: cannot open shared object file` (не JSON): Npgsql пробует GSS-шифрование, а библиотеки Kerberos в образе нет. Безвредно; убирается ключом `GSS Encryption Mode=Disable` в строке подключения (проверено: приложение подключается и мигрирует) — ключ внесён в шаблон Secret.
- Предупреждение хоста при старте: «No XML encryptor configured. Key … may be persisted to storage in unencrypted form» — ключи DataProtection лежат на PVC в открытом виде (защита — доступом к тому; шифрование ключей сертификатом в спринте не задано).
- Необработанное исключение при старте (БД недоступна для `MigrateAsync`): процесс завершается с кодом 139 на Docker Desktop/WSL2; пароль в лог не попадает (строка подключения в сообщениях не выводится). Для Kubernetes важен сам ненулевой код — под перезапускается.
