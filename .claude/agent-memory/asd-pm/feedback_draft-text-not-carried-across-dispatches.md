---
name: draft-text-not-carried-across-dispatches
description: Never claim to "still hold" earlier draft text across a fresh asd-pm dispatch — context resets; ask the dispatcher to re-supply instead of reconstructing.
metadata:
  type: feedback
---

Each asd-pm dispatch starts with an empty context. Draft artefact text (refined scope, AC lists, plan Task sections) presented in an earlier turn is NOT available in a later dispatch unless it was written to a file or restated in the dispatch message. When asked to "print your last-held text", halt and request re-supply — never reconstruct from the slug or from a partial description.

**Why:** during sprint `003-doc-links-and-autonomy` scope phase, a dispatcher asked for the verbatim AC-1..AC-8 set assuming continuity; the sprint folder was still empty (correct — gate before write), so the only copy had evaporated. Inventing the missing ACs would have silently fabricated sprint scope and passed it to the user as recall.

**How to apply:** when a multi-turn approval loop is running against an artefact not yet written to disk, either (a) keep the full current draft in every message back to the dispatcher so it survives the round trip, or (b) get approval to write the skeleton file early and iterate on-disk. Prefer (a) — it does not break the approve-before-write gate. See [[feedback_approval-gate-without-askuserquestion]].
