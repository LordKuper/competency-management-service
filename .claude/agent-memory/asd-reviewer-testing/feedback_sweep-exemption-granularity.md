---
name: sweep-exemption-granularity
description: Canon "no removed name survives" sweeps in tests/run.js - check exemption granularity; a per-line wording skip spans a whole paragraph, and a key-name skip for a same-named state field hides every config reader of it; refuted-claim sweeps keyed on literal phrasings miss variants their own title names
metadata:
  type: feedback
---

When a sprint removes config keys, `tests/run.js` usually gets a sweep over canon asserting no removed name survives. Check how its exemptions are scoped, not only that it is green.

- Canon rule docs write whole paragraphs on one physical line. So a skip like `if (/legacy/i.test(line)) return` exempts the entire paragraph, often the very rule paragraph that used to read the key. Sprint 013 had 13+ canon lines exempted this way, among them `sprint-lifecycle.md` :9, :155 and :345.
- If a removed config key shares its dotted name with a field the state template still freezes (sprint 013: config `documents.c4` vs `t_state.json` `documents.c4`), a "skip what state still carries" rule exempts that key completely. Every config reader of it then goes unguarded. Check whether any other assert fills the gap. In sprint 013 none did: the asd-init step-13/14 asserts only checked that `diagram_tool`/`likec4` were present, never that `documents.c4` was absent.
- Refuted-claim sweeps (sprint 019 iter-02): the regex is built from the exact refuted canon phrasings (`serves a reviewer no write tool`, `only loads it`), so a variant (`no memory write tool`, the one the test title names) passes. Widening it often reddens historical narration in another agent's memory (correctness L10), which is why the tester kept it narrow. Raised below floor there: the homes carry positive clause pins, so the sweep is only a secondary guard. Raise medium only when the row claims the sweep catches "any" reintroduction, or when no positive home pin exists.

**Why:** a sweep with a broad exemption is green today and still green after the reader comes back, so the risk row looks settled when it is not.

**How to apply:** for each exemption, list the lines it currently skips. Grep the exempting token together with the removed names. The sound fix is to pin that skipped set with `deepStrictEqual`, as in [[no-shell-review-method]] "Exemption sets". Raise medium when a skipped line is a former reader site.
