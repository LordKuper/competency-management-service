---
name: fail-first-and-none-honesty
description: This repo's testing reviewer rejects fail-first records that name the wrong assertion, `none` decisions whose stated reason is falsifiable, and assertions that lock wording instead of substance
metadata:
  type: feedback
---

Two things the `impl-review` testing reviewer checks hard in `test-plan.md`, and has raised findings on.

## A fail-first record names the assertion that fires FIRST

Transcribe it from the actual runner output, not from the assertion the mutation was aimed at. A guard
assert placed earlier in the same test body will pre-empt the one you meant to prove; if you want the
later one credited, find a mutation that reaches it (e.g. to prove a chain↔files bijection, add an
orphan file rather than deleting the chain entry, which trips the earlier `must be in the chain` guard).

**Why:** a reader who trusts a wrong record deletes the assertion it names, believing that one carries
the proof, and keeps a test whose actual guard is gone.

**How to apply:** run every mutation — procedure in [[testability-envelope]] "Mutate, run, restore —
one bash call" — and paste the assertion message from the FAIL output into the record. Re-derive
records on re-entry: a dev fix commit can rewrite the implementation out from under an entry-1
mutation.

The converse bites when re-pinning: a red list names only each test's FIRST failing assert, so a
later one in the same body can be red too, hidden. Re-run after every re-pin before calling the set
done (sprint 019 iter-02: the fixed L6212 exposed a sweep hit on a backticked quote in reviewer
memory, committed a round earlier).

## A `none` decision needs a reason that survives inspection

"It's prose interpreted at runtime, no executable surface" is false for anything that is a literal
token in a tracked file (`NEXT: retro`, an ordered arrow chain, a phase-table row, a count word). If
the suite already asserts one such mirror, every comparable one is on the same rung and costs a regex.

"No file restates it, so there is no drift surface to assert" is the other false reason: for a
single-home rule bullet the live risk is deletion, not drift. Sprint 010 entry 1 recorded `none` on
that ground and the testing reviewer routed it back (T-2). A bullet nothing mirrors is exactly the one
nothing pins, and this framework's own economy rule makes deleting unpinned prose an obligation. Two
`assert.ok`s on the rule text, appended to a test that already reads that file, cost nothing and settle
it. Before accepting a no-mirror argument, check whether the preserve-list (or whatever the local
keep-rule is) actually covers that text's class — a procedural instruction is not a contract token, an
enumeration, a case distinction or a stated failure mode, so nothing protects it.

A `none` that says "an absence check would redden a correct reword" is falsifiable too, and a fixed defect
owes a regression proof (`code-style.md` §17) that a `none` must overcome. Scope the absence to the one
clause that carries the claim and to the sub-span that lists the culprit (sprint 022 entry 5: the `;`-clause
naming the DoD gate, the text before `merge mode`), then take the reviewer's own suggested rewrite and a
two-sentence form as reword controls; both stayed green, so the rotated `none` was superseded by an add.

**Why:** TST-01/TST-03 in sprint 007 — the `none` reason was contradicted by assertions the same test
plan had already written.

**How to apply:** before recording `none`, ask whether the thing is a literal token derivable from
something the suite already reads. Reserve `none` for genuine agent-runtime judgement, rendered
appearance, and items owned by a later phase — and for those, name the owner.

## A qualifier-presence assertion is a wording lock — prove otherwise with a reword mutation

When a fix lands as a narrowing qualifier ("sole statement of that scope **in canon**"), the tempting
assert is `includes(qualifier)`. Do not write it: a synonym defeats it and a correct rewording reddens
it, so it fails in both directions while reporting coverage. Assert the *substance* the fix added
instead — for a declaration, the carve-out it now names plus the pointer to the rule that owns the
excluded surface (a pointer is the one machine-decidable property of a declaration here). A prose claim
about config is even better: bind it to the config (`claude.memory === 'project'` for a body that cites
its own `memory: project` grant, per `artifact-layout.md` "Agent memory") — that is a fact, not a phrasing.

**Why:** sprint 010 entry 7. The dev flagged qualifier presence as the only checkable new fact; it was
not, and the qualifier form would have reddened the very next legitimate rewrite of that paragraph.

**How to apply:** run a THIRD mutation beyond fail-first — reword the fixed text end to end, drop the
qualifier, keep the substance, and require the suite to stay GREEN. A red there means the assert locks
wording; record both directions in `test-plan.md`. An order-bound regex (`never[^.]*free-form`) is the same lock:
a correct reword puts the tokens in the other order (sprint 015 C6). Split the text into clauses and test
each token on its own. Related trap from the same entry: a mutation can
change bytes and still change nothing — renaming a cited heading `## Agent memory` to `## Agent memory
directories` left the citation resolving, because heading resolution is prefix-anchored. An anchor guard
catches a missing anchor, never a semantic no-op; confirm the run actually reddens.

A word test on a sentence that also cites a file is vacuous when the word is inside the citation:
`/\bpolicy\b/` matches `` `review-policy.md` ``, so deleting the policy clause stayed green (sprint 021
M18). Strip code spans (`` s.replace(/`[^`]*`/g, '') ``) before testing a prose word next to a citation.
The failure route is the same trap: "a push failure is `FAILED`" satisfies `/\bpush/` after the push
instruction is gone. Drop clauses naming `` `FAILED` `` first, and prove it with a mutation that keeps
that clause (sprint 021 entry 4, M-L).

A *signal-token* presence assert (`includes('\`FAILED\`')`) is the same trap from the other side. It
proves the signal is mentioned, not handled, so "on `FAILED`, continue" passes (sprint 012 external
iter-04 #1). Slice the clause the token opens (to its `;`) and assert each semantic property on its own:
the action (halt), the route (blocker), the ordering (before dispatch). Then mutate each property away
separately, plus a full reversal. See
[[testability-envelope]].
