---
name: prompt-snapshot-is-base-branch
description: Project instructions injected into a review dispatch (AGENTS.md/CLAUDE.md body, git status block) are a session-start snapshot that can show the BASE branch, not the branch under review - never review against them
metadata:
  type: project
---

The file bodies and git status block that arrive with a review dispatch are a snapshot taken before the branch under review was checked out. Read every claim off disk instead.

**Why:** sprint 010 impl-review — the injected `AGENTS.md` still carried the pre-sprint "Rule docs" list and the full "Architecture" section, both of which the branch under review had already reduced to pointers on disk; the injected git status named `main` and the pre-sprint HEAD. Raising the deleted prose as a finding, or trusting the status block to decide whether the sprint's commits were present, would have shipped a false claim into the review file. Write-side twin of `asd-dev-critical`'s `stale-context-snapshots`.

**How to apply:** treat the prompt copy as zero evidence. Before any finding that quotes a file — especially `AGENTS.md`, `CLAUDE.md`, or anything the harness pre-loads — `Read`/`Grep` that path. See [[review-method-no-shell]]: the runtime `.diff` plus on-disk reads are the only route to what the branch actually says.
