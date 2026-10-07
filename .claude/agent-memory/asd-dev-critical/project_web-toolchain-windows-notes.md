---
name: web-toolchain-windows-notes
description: web/ toolchain behaviour on this Windows box - slow first vitest run, no --colors flag, autocrlf vs byte-compared generated files, verbatim-text whitespace lint
metadata:
  type: project
---

Operational facts seen building `web/` (sprint 001, Task 3), not derivable from the code:

- First `vitest run` after a cold start took ~80s (import and environment setup); later runs ~2s. It is not a hang - give the command a long timeout.
- `vitest` rejects `--colors` (unknown option); use `NO_COLOR=1`. Biome takes `--colors=off`.
- `core.autocrlf=true` here. `openapi-typescript --check` compares bytes, so a CRLF checkout of `web/src/api/schema.d.ts` reads as stale; `web/.gitattributes` forces LF for the whole of `web/`.
- In the one-command `git add && git diff --cached --check && git commit --only` form, a failing `--check` stops the chain and leaves the files staged. Fix or `git reset -q -- <paths>` in the very next command (stage-then-wait hazard, see [[parallel-agent-commit-sweep]]). Verbatim third-party text (OFL licences with trailing spaces) is exempted with a `-whitespace` attribute, not edited.

**Why:** each cost a debugging round during the frontend scaffold.

**How to apply:** any later task touching `web/` (regenerating `schema.d.ts`, running tests, committing licence or generated files).
