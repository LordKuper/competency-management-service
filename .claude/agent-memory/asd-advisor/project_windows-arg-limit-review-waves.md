---
name: windows-arg-limit-review-waves
description: Mass rename (~690 files) on Windows hits the 32767-char CreateProcess limit in runtime.js review-waves/emit-manifest (spawnSync git ENAMETOOLONG); method — run unmodified runtime.js in a Linux node+git container for those steps only
metadata:
  type: project
---

On a mass-rename scope (hundreds of files, ~690 seen) impl-review entry fails: `node .asd/runtime.js review-waves` (and `emit-manifest`) pass every scope path plus rename sources to one `spawnSync('git', ...)` (`rangePatchInvocations`/`patchPaths`), ~60k chars, over the Windows 32 767-char command-line limit. Method: run the same unmodified runtime.js inside a Linux `node:<host major>-trixie` container (has git) with the repo bind-mounted, only for the path-list steps; everything else stays on Windows. Log friction + an upstream retro issue (runtime should use `--pathspec-from-file`/chunking).

**Why:** `.asd/runtime.js` is read-only infrastructure during a sprint; a waiver is a hard gate and would not even fix it (list must drop >50%); stopping for upstream is open-ended.

**How to apply:** Any future large-scope sprint on this Windows host (hundreds of changed files, mass renames) will hit the same wall at impl-review entry — recommend the container route up front, and check whether a newer ASD release already fixed the runtime before advising it.
