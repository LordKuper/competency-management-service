---
name: tests-pin-literal-prose
description: older tests/run.js content-contract tests still pin literal rule-doc prose and exact runtime output shapes, so a reworded step or a new manifest field fails a test the dev role may not edit
metadata:
  type: project
---

`tests/run.js` content-contract tests assert exact substrings of rule-doc, workflow and agent prose (e.g. `asd-phase-impl.md` step 6 must contain `sequential where dependent; parallel where independent`, scoped by `initial mode only`; an `asd-dev.md` Tool-policy grant is asserted verbatim via `sync.readNormalized`). Rewording such a line fails the suite even when the new wording is correct.

**Why:** `code-style.md` §17 now says a content-contract test pins only a token that cannot be reworded without changing the contract (heading, command, field name, parsed literal), never the surrounding prose — but tests written before that rule still pin prose. `asd-dev` may never edit or weaken a test, so the test wins the tie.

**How to apply:** before rewording any workflow/rule/agent sentence, grep `tests/run.js` for a distinctive phrase from the line. If it is pinned, build the new sentence *around* the pinned literal; if the pin is prose rather than a §17 token and blocks a correct rewording, list it under `Flagged choices` for impl-test to re-pin. Never edit the assertion. Related: [[feedback_fix-the-class]].

**Second class, not workaroundable — pinned output shapes.** The suite also pins the exact *shape* runtime code emits: some `runtime.js` CLI tests recompute an expected output from a hand-written field set, so an AC that adds an emitted field fails them by construction while neighbouring invariants still pass. No rewording dodges it. Grep `runtime` in `tests/run.js` before changing anything a manifest or CLI output carries; when the collision is unavoidable, implement the spec, leave the test red, and hand impl-test the exact superseded assertion (file:line, what it enumerates, what replaces it) — the DoD is a green suite at the *last* task, not every task.
