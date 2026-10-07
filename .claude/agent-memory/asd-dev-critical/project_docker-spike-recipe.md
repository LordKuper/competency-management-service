---
name: docker-spike-recipe
description: How the Task 10 Docker spike was run on Windows (Git Bash + Docker Desktop) and the host-side gotchas that cost time; results themselves live in the postgres-image tech-reference note
metadata:
  type: project
---

Sprint 001 Task 10 deferred portion (MS-1 done) was run 2026-10-06; results are in `docs/architecture/tech-reference/postgres-image-18.6-trixie.md` ("Проверено на Docker") and the docker/sdk/aspnet/node notes. Not repeated here. What is worth knowing to rerun it:

- **docker.exe is not on PATH** in the tool shell: `export PATH="/c/Users/Michieru/AppData/Local/Programs/DockerDesktop/resources/bin:$PATH"` first (also holds `docker-credential-desktop`).
- **Git Bash mangles `/c/...` arguments** for `docker cp` and `docker exec <path>`: feed files through stdin instead (`docker exec -i ... psql ... < file.sql`); `MSYS_NO_PATHCONV=1` alone does not fix `/c/` style paths, and an in-container path like `/var/lib/...` passed to a host-side `ls` gets rewritten to `C:/Program Files/Git/...` unless that variable is set.
- **A stray `cat > file` with no heredoc blocks the shell on stdin** and the call is moved to the background; write SQL/scripts with the write tool, not inline heredocs, when quoting is tricky.
- **Login is rate limited to 10 requests/minute per IP** (`RateLimiting:Login`): recreate the scratch app with `-e RateLimiting__Login__PermitLimit=1000` before any script that logs in repeatedly (concurrency rounds re-login after every block). The scratch runner is `docker run -d --name cmp-scratch-app --network cmp-scratch-net --cap-drop ALL --security-opt no-new-privileges -p 18080:8080 -v cmp-scratch-keys:/var/lib/competency/keys -e 'ConnectionStrings__Default=Host=cmp-scratch-db;...;GSS Encryption Mode=Disable' -e Bootstrap__AdminUserName=... -e Bootstrap__AdminPassword=...`.
- **Parallel curl rounds**: send with `-b jar` only (no `-c`) so concurrent requests do not write one cookie jar; `curl -w '%{http_code}'` prints no newline, so `cat a b | sort` glues statuses ("200409") - compare against the glued string or add `\n`.
- **Shell `$(call ...)` is a subshell**: variables assigned inside a helper are lost, only fixed file paths survive; get the ETag with a plain (non-subshell) call and read it after, never inside the argument of the call that needs it (it is evaluated before the call runs).
- Docker named volumes copy mode/owner of the image directory on first mount (the postgres image's `/var/lib/postgresql` is 1777), so Docker never shows the `fsGroup` problem Kubernetes would.
- The postgres image declares `VOLUME /var/lib/postgresql`: a `docker run` without a mount there (or with a mount elsewhere, e.g. the wrong-path test) leaves an anonymous volume behind after `docker rm -f`; use `docker rm -fv` or `--rm`, and check `docker volume ls` at cleanup.
- BuildKit with the containerd image store keeps base images by digest after a digest-pinned build: `docker image inspect <tag>` can say "No such image" although the build worked.

**Why:** the spike is expensive to redo blind; these were the stalls.
**How to apply:** a re-dispatch for impl-test Testcontainers or a Dockerfile/manifest change; read the tech-reference note for outcomes first.
