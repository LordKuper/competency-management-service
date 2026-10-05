---
name: testability-envelope
description: What is and is not testable in the ASD framework repo, the two accepted test patterns for its mostly-documentation change surfaces, and the mutation/fixture/authoring traps that keep biting
metadata:
  type: project
---

In this repo (ASD framework source), almost every change is Markdown/YAML/JSON/HTML. `tests/run.js`
is the only runner and there is deliberately no documentation-content harness. Two patterns are the
accepted answer for a mostly-docs change surface:

1. **Explicit `none` decisions** in `test-plan.md` for prose/rule/template edits — first-class per
   `code-style.md` §17, not a gap. Building a doc-content test framework is an over-engineering
   finding, and a new test dependency trips the Simplicity Default.
2. **Static canon-consistency assertions inside `tests/run.js`** where a machine-checkable invariant
   exists across files (derive the value from its SSoT, assert every mirror). Precedents in-file:
   the retired-`asd-pm` canon scan, `upstream_hashes`/`canon_hashes` checks, and the §16 chain
   checks (sprint 007; since sprint 020 sourced from `.asd/workflows/<name>.json` via
   `runtime.loadWorkflow`, one relation per definition). Prefer a new assertion in a
   loop that already reads those files over a new test — the suite count going *down* while coverage
   goes up is a good outcome here.

**Why:** the repo ships no application code, so the naive reading is "nothing is testable"; the real
line is executable Node (`sync.js`, `update.js`, `.asd/migrations/**`, `.asd/runtime.js`,
`.asd/hooks/**`) plus cross-file invariants that a regex can derive. Sprint 007's audit logged both
gaps that pattern 2 closes (G-11 chain mirrors, G-12 forward-only manifest check).

**How to apply:** during the strategy pass, split the change surface into executable vs prose. Give
executable changes real unit tests; give prose changes a `none` with its reason, unless the prose
encodes an ordered/enumerable invariant that mirrors an SSoT — then pattern 2 applies.

Entries below are keyed by topic, never by ordinal: fold a new lesson into the heading it belongs to
instead of appending a numbered one. This file loads on every dispatch of this agent and is reviewed
as a hot path under `artifact-layout.md` "Documentation economy" — ordinal keying is what made it
grow (sprint 010 EFF-2), since appending is always cheaper than merging.

## Mutate, run, restore — one bash call

Helper files (backups, mutation scripts, run logs) go in `.asd/tmp/` (`node .asd/runtime.js scratch-dir`,
`artifact-layout.md` "Scratch directory"), never OS temp or the host scratchpad; no test globs it and it
ignores itself. Back the file up there, then run mutate → suite → restore inside ONE bash call,
restoring in the same call that reads the failure. Never restore with `git checkout --`, and never
park the backup inside a tree a test globs — a stray `.md` under `.asd/rules/` breaks the rule-doc
bijection check itself. For many mutations, a Node script in `.asd/tmp/` (not a heredoc)
that loops `[id, file, from, to]`, checks the anchor hits exactly once, and restores the in-memory
buffer in `finally` with a byte compare worked cleanly (sprint 011, 23 runs in two calls; sprint 021, 40 runs
in the background, ~20 s each). A "revert the fix
commit" mutation built from `execSync('git show <sha>^:<path>')` is a silent no-op on this Windows host:
`execSync` runs through cmd.exe, which eats `^`, so it reads the CURRENT blob. Use `<sha>~1`. Writing regex source into `tests/run.js` from a script: a plain JS string turns `\b` into a backspace byte (invisible in Read, suite goes red) and a global `split('\\`')` strip hits every existing escaped-backtick row in `test-plan.md` — use `String.raw` in a scratch file, or the Edit tool (sprint 014 iter-02). The tell is a
run with zero FAIL lines, not even the hash-ledger noise every real canon mutation produces — unless the script
reads only stdout: `runAll` writes `FAIL -` and the stack to **stderr**, `ok -` and the count to stdout, so a
`spawnSync` loop must parse both or it shows a dropped count with no FAIL lines. For the extra FAIL lines a tracked file's mutation produces:
[[mutation-runs-trip-the-hash-ledger]].

A runner that writes the pre-image to a pending file before each mutation, and restores a leftover pending file at its own
start, makes a killed session recoverable. Classify each run's FAIL lines into ledger noise (`upstream_hashes`,
`canon_hashes`, generated-view `--check`) and your own test, and report only the latter. README sits outside
`upstream_hashes`, so a README mutation shows no ledger line and its green control exits 0; a canon skill or agent trips
`--check` too, so its green control still exits non-zero on noise alone. A suite run is about 20 s: run 60 or more
mutations in the background and poll the log with an until-loop, because a foreground sleep is blocked.

**Why** — the two failure modes this replaces:

- `git checkout -- <file>` re-materialises canon as LF: these files still sit CRLF in the worktree
  from before `.gitattributes` (`* text=auto eol=lf`) landed. Tracked content is unchanged (`git
  status` clean, index `i/lf`), but "restored byte-for-byte" is then only true of tracked content —
  say so rather than overclaiming, and expect a `CRLF will be replaced by LF` warning when staging a
  file that was never checked out.
- A session boundary landing between mutate and restore leaves corrupted canon on disk, and the next
  dispatch inherits it as a diff it did not author (sprint 009 `F-5`). Two things worth knowing when
  that happens. (1) The pair is self-revealing, not silent: an added assertion plus the mutation it
  was aimed at makes the suite RED (the assertion fires), so a claim that "the suite was green with
  the corrupted file" is worth re-checking by reproducing the exact byte state rather than repeating.
  (2) The real exposure is at commit time, not suite time — the danger is a round committed without
  reading the diff. On re-entry into someone else's unrestored work, re-derive every assertion
  against source and re-run every proof: their outputs did not survive, and a proof you did not run
  is not a proof you can record.

## Guards that resist mutation

A purely defensive guard may be unmutatable (e.g. `6.0.0.js`'s `ARCHIVE_DIR` skip is a no-op because
the sprint listing is already non-recursive). Record the limitation instead of claiming a proof that
did not happen.

A precondition guard with no mutable source (a spawn that needs `git` on PATH) is provable by
**environment** instead: re-run the whole suite with `PATH` reduced to node's own directory. Three
external-CLI preflight tests fail alongside it — check which failures are yours before claiming a
test is the suite's only environment-dependent one.

## Scoping a `none` — and a blanket `keep` is the same error

A **blanket `none`** covering several artefacts at once ("the rest is genuinely prose") is read as a
completeness claim and gets rejected. Scope each `none` to one artefact and one named risk, and say
what *would* make it assertable. A `none` whose own risk sentence describes a machine-checkable
literal ("a workflow that never appends") is dishonest by construction. What makes a stated reason
false is [[fail-first-and-none-honesty]]'s subject, not restated here.

A blanket **`keep`** hides the same hole and is easier to miss, because a green run feels like
evidence. "Re-run at this HEAD: every existing pin holds" proves only the pins that already existed —
it says nothing about a contract nothing ever asserted. Sprint 010 entry 4 recorded that row over
another role's five deletions; the continuation found two of the five (the nitpick enumeration and
its `providers.md` grant; the red-full-suite latch invalidation and its two acting sites) had **no**
assertion at any site, before or after. Before writing `keep` on someone else's cut, grep the suite
for the surviving text and the rule it belongs to; if the grep is empty, the row is an `add`.

When a new sweep finds a **pre-existing, out-of-scope** defect, neither ship it red nor drop the sweep:
pin the broken set exactly (`deepStrictEqual` against the known members, both directions) and file the
`D-N` rows, naming the ids in the assert message and stating in `test-plan.md` that fixing the defect
also means deleting its line from the pinned list. Sprint 010 entry 5 did this for two dangling
citations; the local precedent is entry 4's `asd-pm` fallout-set comparison. Shipping red would block
`impl-review` entry on work no round of the sprint touched, and an exemption list with defect ids in it
is visible where a silent filter is not.

Exception: when the stale hits sit in another owner's memory and that owner's memory-fix dispatch runs
*after* the tester chain in the same round (sprint 019 review-fix, COR-1/DOC-1), an exact pin reddens
the moment the fix lands, and nobody left in the round can edit the test. Exempt those files by name
with the finding ids in the assert message (a subset filter, not a `deepStrictEqual`), prove the list is
load-bearing by emptying it, and hand its deletion to the next `impl-test` entry. That entry (019 entry 3)
proves the deletion by appending a refuted line to a formerly exempted file: the sweep must now redden.

At a plain `impl-test` entry there is no such round: ship the new phrase red and file a `D-N` against that memory
file. The test-fix route and the owner's memory-fix dispatch land before the next entry, so no exemption list is needed.
A sweep already red at baseline cannot be proven by a count: parse the `+ actual` hit list from its FAIL block and
require the mutation's own `<file>:<line> <phrase>` in it, and a reword control to leave that list unchanged.

Review-fix tester rows sit in the live tables when the next entry starts, with no `Entry log` row of their
own. Rotate them with the previous entry's rows into its segment (`artifact-layout.md` "Test plan"
Rotation), after carrying each review-fix removal row forward as a live `Removed tests` row that step 5
collects (`asd-phase-impl-test.md` step 4 re-entry). Own practice, no canon: say so in the segment header.

A pin like that carries a **closing obligation**, and it falls to this role because the pin is test text.
When the defect is fixed the exemption line goes with it (say so in `test-plan.md`, since a fix without
the deletion reddens the suite), and then check what the emptied list leaves behind. In sprint 010 it
left two: a stricter tier whose only difference from the general one was refusing to be pinned — now a
subset compared against the same `[]`, so a duplicate §17 prunes, taking its no-longer-guarding floor
with it — and an assert message still explaining what a *missing* entry means, a failure mode `[]`
cannot reach. Prune the tier, keep the rule by moving it into the surviving message as forward
instruction ("if you ever pin one, never on a denial line"), and reshape only if a non-subsumed property
is actually derivable — measure that before claiming it is not. Coverage did not change; the test got
smaller and the exemption became a real assertion. That is the outcome to report plainly rather than
dressing the round up with a new test.

A scope amendment can land while an entry runs (before each commit, run the permitted diff command from the entry's
`HEAD analysed` to current; paths changed beyond the entry's own files mean one landed — or ask the orchestrator through
the payload). When it retires a mechanism, add no
new pin on it (drop the row that reads the retired constant), and list the existing pins it will break, by test name, in
`test-plan.md` for the next entry instead of editing them: the orchestrator ruled the running entry is not interrupted.

Related, when the dispatching message hands you a commit range: check it contains the changes it
names. Entry 4 (cont.) was pointed at `11bf405..dd47159`, which held only sprint bookkeeping — the
dev chain was `5add9f5..2ae44c6`, an ancestor of the entry's own test commit, so the suite run
already recorded had covered it and the range as given would have produced an empty gate. A payload's numeric claim is
the same trap: "61 lines already changed in tests/run.js" (entry 3) was entry 2's own commit, outside the delta. Run
`git diff <prior>...HEAD -- <file>` before trusting it.

## Match the mutation to what the assertion claims

An assertion late in a multi-fixture test is only proven by a mutation that leaves the earlier
fixtures passing. Changing *which* key/branch the code touches (e.g. top-level `delete` → recursive
strip) reaches it; a wholesale pre-fix restore does not.

A wiring-level mutation can be pre-empted by an earlier scenario of the same test when both share the mutated input
(fixing the file count to one reddened the byte-axis scenario first, so the file-axis assertion was never reached). Aim at
the function the earlier scenario does not depend on, and read the first failing message before crediting it.

An "at least one example without X" assertion (template conditionality) is only proven by a mutation
that adds X to **every** remaining block — a single-site edit leaves the claim true and the mutation
uncaught.

The converse applies to a loop over a derived set, such as "each value type in `t_config.yaml`". Reverting
the whole fix fires only on the set's first member, in insertion order, so it proves just that member.
Add a second mutation that removes only a later member's clause before you record that the loop proves
every member (sprint 012 entry 4, M26/M27).

A `keep` justified by "the existing assert already requires it verbatim" needs the fix's own revert
as proof. A regex whose capture group spans less than the fixed literal (sprint 015 AC-8 captured only
the suffix, which the pre-fix line also held) passes on the old text too. Put the capture around the
whole changed literal, then run the revert (TST-3). Same for a fixture aimed at a narrowed regex: it
must satisfy the OLD regex's precondition too, or it passes both sides (sprint 019: a `tableCells` fixture
not starting with a backtick never reached the changed branch). Some edits are semantic no-ops you cannot
mutate with: `git commit -- <paths>` already implies `--only`, so dropping only `--only` changes nothing.

A render property of tier variants (`-mechanical`/`-critical`) is only proven by rendering, never by reading
the generated `.codex/`/`.claude/` views: a `variantMeta` regression leaves the committed views untouched,
so a disk read stays green and only §9's `--check` drift fires. `sync.buildSyncPlan(REPO_ROOT)` items carry
the variant meta as `metaOverride` (`agentVariants` is not exported) — render those (sprint 018).

A mutation that also reddens an OLDER sprint's test shows your new assert repeats part of it. Drop that half and keep
only the new relation. Then re-prove with a mutation that reaches only yours (sprint 022 entry 2: the rollback
`re-divides` token was already sprint-017 D3's, and only the shared `waves.json` span was new).

A "tight somewhere" assert on an upper bound only catches an overcount that hits every sampled point.
When a fix adds a parameter with a default, the old and new formulas usually agree only at some inputs
(sprint 015 EXT-4: only at multiples of 25). Compute both at each sampled point before writing
"the default is unchanged", and pin tightness at exactly those points (mutation: loosen only there).

A reword that mentions a pinned literal a second time in the same sentence de-pins its first mention: `sentence.includes(token)`
stays green with the original mention deleted. After any dev reword of a pinned sentence, run the old literal-drop mutation
against the UNCHANGED tests; a green run is the evidence, and the fix keys the assert to the trigger's position (the text
before a later pinned token), never to the whole sentence. A guard written `block === -1 || order holds` is vacuous when no
fixture block carries both items: confirm one does before crediting it as the pin on an order.

## Assert removed phrases, not topic words

Rule prose here routinely narrates the alternative it just rejected inside the same bullet
(`asd-phase-impl.md`'s fix-mode line explains why parallel rounds were dropped). A bare keyword
absence check (`!/parallel/i`) therefore goes red against unmutated HEAD. Assert the absence of the
specific *removed instruction phrases*, never of a topic word.

A defect the manual leftover check found (no runner line) still owes a §17 regression proof: append its
fix commit's exact removed phrase to the sprint's leftover-sweep list, then restore the `<sha>~1` blob to
prove it (sprint 021 D-4). Widening an actor rule ("a sentence naming `--apply` must deny it") to all memory
instead false-hits factual mentions of the command.

A removed-phrase sweep outlives its sprint, so a later sprint can legitimately re-adopt a banned literal. Sprint 021
banned the `done` pr exits and sprint 022 made `done` the exit. Delete those entries and cite the audit
Contradictions ruling as the reason. Do not exempt them: an exemption keeps a ban on text that is now canon.

Build the phrase list from the removed lines of the pre-sprint diff, then validate every entry with a scratch script: it
must occur at the pre-sprint revision (`git grep -nF -e <phrase> <rev> -- <paths>`) and nowhere in the worktree now. A
phrase that fails the first half was never removed, one that fails the second is a live leftover to route. A mutation that
restores old text also trips this sweep, so prove a relation test with a differently spelled token (misspell the carve-out
word) and keep the old-text restore for the sweep's own proof. A per-line co-occurrence check ("every no-tests line names
the carve-out") is satisfied by any occurrence on a long paragraph line; record that ceiling or scope it per sentence.

Same family, for locating a sentence: key the locator to the sentence's **citation**, never its
ordinal or adverb. `sprint-lifecycle.md`'s latch-clearing route was renamed "A THIRD" → "A further"
mid-sprint; a `find` on the citation (`` `review-policy.md` "Late duplicate return" ``) survives
that, an ordinal-keyed one reddens on a correct edit.

Table rows likewise: a `^\| design \|` row locator hits `sprint-lifecycle.md`'s phase table before
the no-op table. Anchor on the table's header row (`\| Phase \| No-op when \|[\s\S]*?^\| design \|`).

Co-occurrence sweeps ("a line quoting a workflow name may not say default") scope per **sentence**, not
per line: canon lines hold several sentences, and an unrelated "resume (default)" shares a line with a
quoted `standard` (sprint 020 review-fix). Re-measure any reviewer claim like "a line-scoped widening
needs no exemption" at the current HEAD: the dev chain in the same round can make it false.

A reviewer-requested cleanup of hook/runtime code (dropping comments, collapsing a branch) is a
behaviour change candidate: mutate the rewritten line. Sprint 020 entry 3 found three slips on the
archived-sprint filter that no test caught.

## Citations and "not restated here" are checkable, as relations

Two classes that look like unassertable prose and are not.

A **cross-file citation** (`` `asd-advisor.md` Don'ts ``) names a section that either does or does not
hold the rule. The general form is a repo-wide link checker — new infrastructure, correctly declined —
but that argument only rules out the general form. The specific one is a derivation: scan the target's
candidate sections for the rule, assert exactly one holds it, then assert the citing line names *that*
heading. Reword either side freely; it goes red only when the rule moves without its citation. Do not
record `none` here on "it would be a link checker". A citation check scoped to a whole workflow step is already satisfied by a
sibling bullet that cites the same home: when a fix adds the pointer inside one sub-bullet, locate that bullet by its bold label,
assert on its own line, and take the pre-fix bullet as the mutation; measure first that the step-wide check stays green on it.

A **"not restated here" declaration** is checkable as a pair at each site: assert the bullet still
spells the mechanic out (the restatement is load-bearing at the acting step), AND assert the denial is
absent. The positive half is what keeps the negative half from going vacuous under a rewording. Sprint
010 iter-02 found three such declarations false at the moment they were written — the same bullet
restated the mechanic one clause later — and a false denial reads as licence to delete the SSoT copy,
which is the one an agent that never opens that workflow depends on. Prove these by literally
reverting the fix commit, not by a synthetic edit. Sprint 010 iter-03 decided the limit of this: whether
a denial is *true* is a paraphrase judgement with no derivable proxy over this corpus, so the pair-pin
per touched site is the whole answer — record that as the `none`, with the measurement behind it.

The *pointer* half generalizes to the whole corpus and is worth one sweep, not one test per fix.
Sprint 010 iter-03 replaced its own denial-scoped resolver with it: match `` `<file>.md` "<Section>" ``
across `canonMarkdownFiles()`, resolve base name → repo root → `.asd/templates/t_<base>` (so `AGENTS.md`
and `audit.md` resolve with no hardcoded pair), and accept the target as a `## heading` **or** a
`**bold label**` — 10 of canon's 185 citations name a bold label, `sprint-lifecycle.md` "Impl-review
clean-worktree precondition" among them, so a heading-only check reddens on correct edits. Match
headings prefix-anchored: `## Related open stubs (optional)` is cited without its parenthetical. A
two-label citation (`` `x.md` "PR phase" "Merged-unclosed" ``) is resolved on its first label only; do
not claim the sweep covers the second (sprint 021 entry 3). It found
two dangling pointers (`checkpoints.md` "Re-running a phase", `external-review.md` "Iteration-aware
diff"), both from PR #25 renames whose two sibling citations sprint 006's documentation reviewer fixed by
hand; the class is recurrent and human review catches it only partly. Both were fixed at `ac3073a` and the
sweep now compares against an empty set — tier the result only while an exemption exists (see the closing
obligation above), and prove a retarget by **reverting each half as a mutation**: a pointer fix is
checkable only by resolving it, and a literal pin on the new heading would just redden the next correct
rename.

## Pin the relation between two sites

When a canon fix invalidates an assertion, re-pin the **relation between two sites**, never a fresh
literal on one of them. Sprint 009 iter-02 — `checkpoints.md` counts fix rounds by a tail match while
`asd-phase-impl.md` step 11 emits the whole heading; the durable check derives the emitted literal
from the workflow and asserts it *ends with* the tail read out of `checkpoints.md`, so either side
may be reworded freely as long as the counter still selects the emitter. String equality between the
two would have gone red on the correct fix, exactly as it did.

A placement order stated at several sites (the home rule, a template format rule, the sibling declarations) is a relation of
code spans: each site names the neighbour it is ordered against, one site lists the whole order and is compared as a list, and a
direction word is checked on the connector text between the previous span and the neighbour with a negative vocabulary
(`under|below|after|behind`). Record the ceiling: a reversal by a synonym outside that vocabulary passes. A mirror line that says
"same placement" names no span, so no per-line check reads it and a chain of them computes the reverse order: pin every sibling
mirror in the same pass, its spans before the first `;` (minus its own declaration span) against the home's list read from the
home, because an unread mirror recorded as a ceiling is the defect a later review finds. A stop-condition list is
sliced per `;` clause, one assert per property: the clause that gates on a signal names the phase it binds, and the carve-out has
its own clause carrying the signal and the pointer to the rule that owns it.

A substring token relation (`site.includes(token)`) goes vacuous when the token already sits inside a
longer span on the target: sprint 021 entry 5's new closure-write field `pr.number` was "named" by scope's
older `` `gh pr view <pr.number>` ``, so reverting the fix stayed green. Compare code span to span
(`spans(site).includes(token)`) for a field the fix adds.

A fix that makes one site produce the value another site gates on ("commit `phase=pr` so Step 1's `phase="pr"`
gate matches base") is pinned by deriving the gate's value from the gating site's code span and requiring the
producer to write it (quotes ignored). Mutating only the gate proves the derivation (sprint 021 entry 6, M-AG). A "first matching span in the section"
derivation re-sources silently: when canon removed that gate and added the same token elsewhere in the section, the
test stayed green on the wrong site (entry 7). Key the derivation to the clause that states the relation.
A fix that *removes* a narrowing condition has no positive substance: require its clause to lack the condition's
one word (`null`), keep the positive assert on the clause itself so deletion cannot pass vacuously, and record
both ceilings (a synonym passes; "whether or not null" reddens).

A **value mirror** (one tier stated in agent frontmatter, README tables and two prose sentences) is a relation, not a
literal. Take the effective value from `sync.buildSyncPlan` metas (`variantMeta` already drops a variant's unset effort; do
not re-derive it), compare structured cells exactly, and compare prose as the whole-word vocabulary set (families plus
efforts) of the one sentence that states it, so a reword stays green. Record the ceiling: pairings inside a sentence are not
compared. Find that sentence after dropping table and heading lines: a table has no full stop, so it reads as one sentence
and the locator hits twice. When the rule doc's own tier table is deleted, the frontmatter becomes the only home and the
mirror test shrinks to README against frontmatter: delete the table read and every assert message that names it, and keep
the sandbox expectation for the read-only agents as a literal rule checked against frontmatter.

A **concrete-id bump** (sprint 022 entry 4, `sol` to `gpt-6.1-sol`) lands in four kinds of pin. A byte-for-byte fixture's
`content_digest` covers the id line: recompute it with `node:crypto` over the edited body and check it equals the runner's,
keep the fixture literal, and derive every other expectation from `model_families`. A literal absence guard
(`!includes('gpt-6-sol')`) cannot see `gpt-6.1-sol`, so guard the shape. An override proving "the table you pass wins" must be
an id no live table can hold (`gpt-0-sol`), or the live lookup satisfies it. An `includes` placed after a `strictEqual`
against the same fixture is unreachable, so delete it.

## Fixtures whose bytes are the input

Never commit one whose distinguishing bytes cannot survive checkout. Sprint 009 shipped
`demo-agent.crlf-bom.md` as the CRLF/BOM input; its blob carried zero CR from day one and the CRLF
came entirely from `core.autocrlf=true` converting on checkout, so the same sprint's `.gitattributes`
(`* text=auto eol=lf`) made the test fail its own sanity assert on every clean clone, at 170/171.
Committing real CRLF bytes plus a `-text` override is the trap answer: it re-breaks the `every
tracked blob must be LF in the index` assertion AND makes `git diff --cached --check` report trailing
whitespace on every line. Build such an input in the test body instead (read the clean fixture,
hard-normalize to LF, re-expand, prepend the BOM, write under `mkTempDir()`), and assert against
doubled CRs so the construction is correct in a CRLF working tree too.

**Why:** any assertion about the bytes on disk is really an assertion about checkout configuration
unless the test produces those bytes itself — and a suite run inside a stale working tree cannot see
it.

**How to apply:** when a test opens with a "fixture sanity" assert about line endings, encoding or a
BOM, treat that as the signal and move the construction into the test. Verify with
`git show HEAD:<path> | od -c`, not by reading the worktree copy. Two corollaries: this is why a green
suite in a long-lived worktree is not evidence about a fresh clone, and mutating `normalizeText` to
prove such a test never reaches the output-equality assertion — CRLF and BOM each break the frontmatter
fence first, so record the thrown parse error as the first failure instead of claiming the assertion
you aimed at.

Same family for a temp git repo fixture: the host's global/system git config is also an input. The
test helper's `-c` flags do not reach the git that `runtime.js` spawns, so pass
`env: { ...process.env, GIT_CONFIG_NOSYSTEM: '1', GIT_CONFIG_GLOBAL: <empty temp file> }` to the
test's own git AND to every `runtimeCli` call (sprint 015 TST-2). To prove it, run the suite with
`GIT_CONFIG_GLOBAL` pointing at a file that sets `diff.noprefix = true`: without the isolation, the
`diff --git a/ b/` header match fails.

## Backward-compatibility fixtures

A fixture built by calling the function under test is not a fixture. Sprint 009's legacy-manifest row
stamped its digest with `coverageManifestDigest` itself, so it tracked whatever that function did and
stayed green straight through the identity break it claimed to cover. Build a legacy artefact from the
*untouched primitive* the old code used (`runtime.fingerprint` + the old key handling), so it stays
frozen at the old behaviour when the current one changes.

## Fills and guards that hide the fixture

When a test exercises a published constant by filling its placeholders, build the filled copy from the
constant's OWN entries (`Object.fromEntries(Object.entries(x).map(...))`), never
`Object.assign({}, x, {i: …, p: …})`. Sprint 010's row-example test used the second form, so it
supplied `p` whether or not the constant carried one — the mutation that dropped `p` from
`LEDGER_ROW_EXAMPLE` passed green and revealed the test, not the code. Run the mutation before
believing the assertion; a green mutation is a finding about the test.

Same family for *guards*: a guard asserting a field is absent from a canon fixture must key on the
provider-scoped literal. `demo-agent.md` carries `"model"` in both its `claude` and `codex` blocks, so
`!canon.includes('"model"')` is red at HEAD however correct the mutation was;
`!canon.includes('"model": "opus"')` is the assertion meant. Run the suite once after adding a guard,
before recording anything about the assertion it protects.

A fixture sized to a limit that later moves can stay green while its stated purpose stops being true: a numeric string equal
to the old byte limit coerced to a count of exactly one, the silent collapse its message names, and at the new limit it
coerces to two. Derive such inputs from the exported symbol, then grep the suite for the old number as a leftover. For a
retuned single-home value (a threshold, a turn cap) measure before writing `none`: revert the value alone against the
unchanged suite and read the own-FAIL list. Empty means no test pins the value, the intended state for a tunable
(`code-style.md` §12) and a ceiling to state. A canon-only revert of a cap that a generated view mirrors reddens just the
render mirror and `--check`.

## Sweep guards: row set and exemption set both

A **reach** claim ("this rule reaches every role that authors X") is not agent-runtime judgement — it
is a sweep of `providers.md` "Role-scoped context". Each row's Additional-context cell is a fixed grant
list, so a rule's home being granted is a literal check; the one row that grants "files named by the
consulting question" (`asd-advisor`) is a derivable exemption, not a hardcoded one. Guard the loop with
a row-count assert: a regex that stops matching the table makes every grant assertion pass vacuously.

The row-count assert protects only the row set; the exemption set needs its own comparison. That reach
sweep skipped any row matching the advisor's wording, so any number of rows adopting that wording would
have dropped out green while `test-plan.md` asserted the exemption was singular (sprint 010 T-3).
Compare the derived exemption list to the expected one exactly, then loop the remainder.

Converse trap: one `deepStrictEqual` against an expected list absorbs *two* failure modes and reports
whichever fires under a single message. A guard written `deepStrictEqual(holders, ["Don'ts"])` covers
both "stated in more than one section" and "stated in the wrong one", so the second arrives labelled as
the first and the reader deletes the wrong assertion. Split it: a `length === 1` guard with the
uniqueness message, then the identity check with its own. The mutation is what surfaces this — a
mutation whose FAIL message does not describe what you just did is a finding about the test.

## Widening a scoped assertion

Check the tree before generalizing one directory to a whole tree. Widening the agent-memory index-link
test over all of `.claude/agent-memory/**` goes red at HEAD on `asd-pm/MEMORY.md`'s dangling
`feedback_flag-gate-semantics-before-applying.md` link — pre-existing, outside any current change
surface, and `asd-pm` is no longer in the agent roster. Sprint 010 iter-02 (TST-01) answer: derive the
loop's set from `.claude/agents/*.md` — the dispatchable agents, tier variants included — rather than
hardcoding directory names or globbing the memory tree. A new agent's directory is then covered the day
it appears, and a directory no agent can load falls out by construction rather than by an allow-list;
compare that fallout set to its expected members so a misspelled directory cannot join it silently.

## A canon shell command is executable evidence

A rule that tells the orchestrator to run a literal command (`git log --format='…%(trailers:key=ASD-Task,valueonly)' <anchor>..HEAD -- . ':!…'`)
is testable by extracting it from the rule and running it with `execFileSync('git', …)` in a `mkTempDir()`
repo (pass `-c user.name/-c user.email/-c commit.gpgsign=false`). Tokenize with `/(?:[^\s']+|'[^']*')+/g`
then strip `'` — the naive `'[^']*'|\S+` splits `--format='%h %s'` at the space and git reports a bad
revision (sprint 014 entry 1). Derive the trailer key from its defining rule, not the command, so the
run also proves the two sites agree. The fixture has to contain every commit shape the rule makes a claim
about. With one `src/` commit and one trailer, a pathspec re-added to the command, or trailers joined onto one line,
stayed green. Entry 2 added a `.asd/sprints/**`-only commit and a two-trailer commit, and both
mutations turned red.

A rule that gives a fast-forward as two commands by case (base checked out versus not) is testable the same way with a temp
origin and clone: the refspec fetch moves the ref alone on a dirty sprint branch, is refused into a checked-out branch, the
plain fetch plus `--ff-only` merge moves the worktree, and both refuse a diverged base. Locate the merge span by its
leading `git merge`, not by `--ff-only`, so a mutation that drops the flag reaches the refusal assertion instead of the
locator. Run `execFileSync` with `stdio: 'pipe'` or a refusal's stderr leaks into the suite output.

A runtime check on the staged diff (a memory content check) is one sandbox repo walked in order: placeholders pass,
an unstaged violation is ignored, a legacy line kept or deleted is ignored, a violation outside the tree is ignored, then
the hits with their line numbers, the same run from a subdirectory, the bad-input exit, and a fresh repo for the file-name
hit. One mutation per branch of the scanner is what the walk proves.

## `routeTask` has no plan-file parser

`runtime.js` `routeTask` takes a structured input object; the `Material risk` extraction is the
orchestrator's. Any proposed test of plan-grammar routing "through route-task" is unfalsifiable by
construction — record it as a checked-and-false premise rather than writing a test that only proves a
pure function is deterministic.

## The terminal gate covers what every per-entry record cannot

An impl-test entry records its run at the HEAD it *analysed* — the tree before its own test commit
exists. So the last per-entry `Suite run` row is always one commit short of the tests it added, and
the `impl-review` terminal gate is the first (and only) run at a HEAD that includes them. Say that
explicitly in the record instead of writing the gate as a redundant re-run; sprint 010's superseded
row sat at `93f8a20` while its assertions landed in `596ef18`. An unchanged count across that delta
is a real result, not a no-op: assertions added to existing tests never move it.

**Why:** a reader comparing two identical counts concludes the gate proved nothing, when what it
proved is that the entry's own commit is green — which nothing else in the sprint ever checks.

**How to apply:** at gate time diff the recorded HEAD against current (`git diff --name-status <recorded>..HEAD`),
name the paths the earlier record could not cover (commit subjects come from the orchestrator through the payload),
and state whether the count moved and why. Note
also that no test reads this repo's live `.asd/sprints/**` — every sprint reference in `tests/run.js`
is a temp-root fixture — so editing `test-plan.md` cannot change the suite result and needs no re-run.

## Retired mechanism under an existing test: rewrite, do not delete

`asd-phase-impl-test.md` step 5 classifies a removal as in-scope only when the *test file* is in the change
surface, and `tests/run.js` almost never is — so deleting any test whose mechanism a sprint retired trips the
out-of-scope removal gate. Sprint 012 met this with the sprint-008 split test, which hand-built halves with
`manifest-digest --write` after `emit-manifest` replaced that procedure: rewritten in place against the new
seam and recorded as `keep (rewritten in place)`, one duplicate assert dropped with its reason in the row.
Same move for a test carrying a local copy of a derivation the runtime now owns (the documentation-economy
test's rubric parser): point it at the runtime, don't keep two parsers.

A test whose WHOLE subject the sprint retired is another case. Once an earlier entry's own commits put `tests/run.js`
in the sprint's overall surface (`git diff <base>...HEAD --stat -- tests/run.js` lists it), deleting it is in-scope: say
so with that evidence and flag the reading. Before deleting, read each assert for a property of a KEPT helper it pinned
by accident: a retired CLI test was the only one feeding the shared list reader a CRLF list with a trailing blank line.
Move that fixture into a surviving test (the list writer, and compare names, not the length) and mutate the reader.
`split('\n')` alone is an equivalent mutant, because `trim()` already strips the `\r`; drop both.

## Authoring `tests/run.js`

It gets reviewed against `code-style.md` §7, which forbids in-body comments with no framework
exemption — the ~60 pre-existing ones are not a licence, and new ones draw a Documentation finding
every time. Put the reasoning in the `assert` message; it is read at the moment of failure, which a
comment above the line is not.

When the thing under test *throws* where you assert a value, catch it into the compared value
(`verdict = \`rejected: ${error.message}\``) — the failure then prints your assert message plus the
real reason, instead of a bare stack from inside the library.

## Heredoc backslash mangling

The bash tool mangles a backslash inside a quoted heredoc, so a python/JS patch script piped as
`python - <<'PY'` turns `\'` into `'` and its anchor silently stops matching an escaped apostrophe in
the target file. Write the patch script with the Write tool and run it by path, or pick anchors with no
backslashes; and for new assertion text prefer JS double-quoted strings where the message contains an
apostrophe. Same call also fails outright ("unexpected EOF") on some longer heredocs — the file never
gets written, so nothing is half-applied, but do not retry blindly.
