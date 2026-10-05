---
responsibility:
  owns: per-sprint registry of manual operational actions a human must perform for the sprint plan to complete
  excludes: code todo stubs (stubs.md), manual QA verification of behaviour (reviews testing.md), plan tasks (plan.md)
  delegates_to: stubs.md (code stubs), plan.md (tasks + BLOCKED markers), reviews/ (manual verification of behaviour)
---

# Manual Steps

Definition, boundary against `stubs.md` and manual verification, validation duty, status transition and lifecycle: `artifact-layout.md` "Manual steps" (sole SSoT, not restated here). Entries are append-only; entry content is `language.docs`.

## Summary

| ID | Title | Blocks | Performed by | Status |
|---|---|---|---|---|
| MS-1 | Установить Docker Desktop (WSL2) на dev-машину и проверить, что `docker version` отвечает | Task 10 — подзадача проверки на Docker (`docker build`, linux-x64 записи в `web/package-lock.json`, spike на `postgres:18.6-trixie`); impl-test (Testcontainers) | user | pending |

## MS-1 — Установить Docker Desktop (WSL2) на dev-машину и проверить, что `docker version` отвечает

- **Blocks**: Task 10 — подзадача «проверка на Docker» (`docker build` образа `app`, регенерация `web/package-lock.json` в linux-контейнере при отсутствии linux-x64 записей, spike на неизменённом `postgres:18.6-trixie`); impl-test — тесты на Testcontainers.
- **Why**: AC-4 (воспроизводимая упаковка проверяется сборкой образа) и AC-3 (миграции `pg_trgm`, Identity, триггер журнала не исполнялись против реального PostgreSQL 18 — `plan.md` Risks). Без Docker ни то, ни другое не проверить; на машине нет ни Docker, ни PostgreSQL, ни `kubectl`.
- **When**: до последней подзадачи Task 10 (волна 8) и до impl-test.
- **Prerequisites**: Windows 11 с включённой виртуализацией (BIOS/UEFI), права администратора, доступ в интернет (образы тянутся с Docker Hub и `mcr.microsoft.com`).
- **Performed by**: user
- **Status**: pending

### Steps

1. В PowerShell от администратора выполнить `wsl --install` (если WSL2 ещё нет) и перезагрузить компьютер.
2. Скачать Docker Desktop для Windows с https://www.docker.com/products/docker-desktop/ и установить, оставив включённым backend WSL 2. Условия лицензии Docker Desktop для вашей организации проверяет пользователь.
3. Запустить Docker Desktop, оставить режим Linux-контейнеров и дождаться состояния «Engine running».
4. В новом терминале выполнить `docker version` и `docker run --rm hello-world`.

### Verification

`docker version` печатает обе секции, `Client` и `Server`, у `Server` — `OS/Arch: linux/amd64`; `docker run --rm hello-world` завершается успешно (в выводе `Hello from Docker!`). Dev, получив подтверждение, запускает обе команды сам и только после этого переводит статус в `done`.
