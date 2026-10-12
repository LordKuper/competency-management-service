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

- `vite.config.ts` `chunkSizeWarningLimit` sits just above the single bundle (raised to 1600 kB when @dnd-kit took it to ~1.54 MB); a new runtime dependency can cross it, so check the build tail for the chunk warning and raise the limit with the comment.
- `npx biome ci <paths> 2>&1 | tail -3` shows only the closing `Checked N files ... No fixes applied.` lines and hides the format diff printed above them, so a failing file looks clean. Read the exit code (`; echo "exit $?"`) or the whole output before committing.
- `tsconfig.json` typechecks the test files too, so `npm run lint` and `npm run build` go red when production code removes an export or adds a required response field a typed test fixture lacks; `npx vite build` alone shows whether the production bundle builds.

**Why:** each cost a debugging round during the frontend scaffold.

**How to apply:** any later task touching `web/` (regenerating `schema.d.ts`, running tests, committing licence or generated files).
