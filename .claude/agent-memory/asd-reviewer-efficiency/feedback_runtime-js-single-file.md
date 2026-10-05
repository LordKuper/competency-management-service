---
name: runtime-js-single-file
description: Standing user override — do not raise SC-1 (god/sprawling type) against .asd/runtime.js; it stays one file by decision
metadata:
  type: feedback
---

Never re-raise an SC-1 (god/sprawling module) split finding against `.asd/runtime.js`, even though it mixes coverage-ledger validation, external-CLI preflight/negative cache, and task routing.

**Why:** a prior sprint-006 impl-review iteration raised exactly that split; the user overrode it and ruled the file stays single. Re-raising a settled override burns an iteration and erodes the value of the undroppable-critical convention.

**How to apply:** in any impl-review touching `.asd/runtime.js`, judge only what the new code does (new subcommands, new fields, dead leftovers) — mark SC-1 `pass` for that file. The override covers the split only; ordinary over-engineering findings inside the file (dead fields, unused returns, no-op spreads) are still fair game and were accepted as valid in iteration 2.
