---
name: reviewer-write-scope-declaration
description: Resolved shape (sprint 010 iter-05, sprint 019 AC-14 answer b, sprint 021) of review-policy.md "Gate Verdict Format"'s sole-statement declaration — bounded to canon, agent memory excluded, a reviewer writes only its own memory plus (internal reviewer) its payload's return file, policy-bounded; a memory finding routed to its memory-fix dispatch; which restating sites are authorized and must not be re-raised
metadata:
  type: project
---

`review-policy.md` "Gate Verdict Format" owns the reviewer read-only reconciliation: what the
read-only claim covers and how a reviewer's memory is written. A reviewer writes only its own memory
directory and, for an internal reviewer, the return file its payload names ("Coverage ledger"
Persistence; added sprint 021). Policy, not the host, keeps it there. On Claude `memory: project`
serves every reviewer `Write` (no reviewer's `disallowedTools` names it); on Codex the internal
reviewers run `sandbox_mode: "workspace-write"`, which cannot be path-scoped (`providers.md`). A finding
located in a reviewer's memory goes to that reviewer's memory-fix dispatch (`review-policy.md`
"Autofix vs escalation"); the orchestrator commits memory writes ("Diff reachability").
Since sprint 010 iter-05 the declaration is **bounded to canon** and names hand-authored agent memory
as outside its reach (`artifact-layout.md` "Agent memory"). These sites legitimately restate or scope
it and are non-contradicting at sprint 019 HEAD. Do not raise them as SSoT duplication:

- `.claude/agent-memory/**`: outside canon by the declaration's own exclusion, so a memory file may
  restate it. A real finding there routes to the memory's owner through the memory-fix dispatch. A
  non-owner never authors memory text.
- `README.md`: a user-facing mirror with no canonical source under `.asd/`. It cites
  `review-policy.md` for scope, says `memory: project` serves reviewers `Write` kept to their own memory
  by policy, and names the memory-fix dispatch. The AGENTS.md cross-file list authorizes it as a roster
  mirror. Its Codex sandbox claim must track `providers.md` (internal reviewers `workspace-write` since
  sprint 021); a stale `read-only` there is roster drift, not duplication.
- `.asd/agents/asd-external-review.md` tool policy: its "no file writes at all" is a separate,
  transport-level rule for the review itself. The same bullet names the one exception, its own memory
  written with the `Write` `memory: project` serves on Claude, and routes a finding there through the
  memory-fix dispatch. It points at "Autofix vs escalation" and cites "Gate Verdict Format" for scope,
  so it is not a second statement of the reviewer-class scope. Its `Bash` runs only the wrapped CLI;
  its Codex `sandbox_mode` stays `"read-only"`.
- `providers.md` "Reviewer agents carry no artifact-write grant…": owns only the tool-grant facts
  (`memory: project` adds `Write` to every reviewer on Claude; Codex internal reviewers run
  `workspace-write`, policy-bounded) and cites Gate Verdict Format for what the read-only claim covers,
  including the memory directory and return file it leaves writable. A test checks that citation and
  checks that the file holds no copy of the MEMORY-FIX contract.

**Why:** SSoT findings use this declaration as their premise, so a false declaration is a trap. Iters 03
and 04 each disproved an earlier, wider version of it (first the workflows, then a reviewer's own
memory file), and sprint 019 iter-01 disproved its "reviewers hold no memory write tool" premise. The
accepted fix is to narrow or correct the declaration, not to delete the acting sites
(`asd-dev-critical`'s `feedback_false-ssot-declarations.md`).

**How to apply:** before flagging a restatement of reviewer write scope, check whether the site is canon
(`.asd/**`). Also check whether it states the *class-level* scope or only the write it performs itself.
Leftover imprecision at a non-canon site is below critical at most. `tests/run.js` AC-15 checks the
substance, not the "in canon" wording, so a correct rewording stays green. It checks the agent-memory
carve-out plus the `artifact-layout.md` pointer, the memory-fix dispatch plus the "Autofix vs escalation"
pointer, the external agent's bullet naming its own-memory `Write`, and on all five reviewers
`claude.memory === 'project'` with no `Write` in `disallowedTools`. The sprint 019 AC-14/AC-16 sweep
fails on any canon, README, AGENTS.md or agent-memory line (orphan agent directories included) that
still denies a reviewer its memory `Write`. See [[no-shell-doc-review-method]] and
[[external-review-prompt-duplication]].
