---
name: doc-economy-pinned-clauses
description: before raising a Documentation-economy restatement finding on a pointer-plus-outcome clause, read the suite assertion and the plan decision that name it; a pinned clause is deliberate
metadata:
  type: feedback
---

A skill or workflow step that points at a rule home and also restates that home's failure outcomes looks like a
"fact whose home is another file" cut. It may be required on purpose: a suite assertion can demand the outcomes in the
step so an offline or sandboxed run has a rule when the pointed-to fetch cannot run.

**Why:** a restatement flagged on sight would have cost a full fix round for a clause the tests deliberately pin, and the
assertion's own message states the failure it prevents.

**How to apply:**
- Before a restatement finding, Grep `tests/run.js` for the step's heading or a distinctive phrase and read the assertion
  message; if it names a failure the clause prevents, keep the clause and say so in the review basis.
- Check the plan's design decision for the site: "cites the home" and "restates the outcomes" are different decisions.
- A finding that survives both checks is a real economy violation; name the pin the fix must also update.
- A pointer that names a rule home for a case the home does not cover (an id list, a mode list) is a better finding than
  a restatement: grep the home for each case the pointing site claims.
