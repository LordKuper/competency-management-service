---
name: parallel-agent-commit-sweep
description: Shared worktree in parallel impl dispatches - a sibling commit can swallow your uncommitted edits, and git stash sweeps away everyone else's work
metadata:
  type: project
---

When `impl` dispatches several tasks concurrently on one working tree, a sibling agent that stages broadly (`git add -A` / `git commit -a`) commits **your** uncommitted files under **its** message. Observed 2026-09-07 in sprint 008: Task 6's commit carried Task 9's whole `.asd/runtime.js` change, so Task 9's own commit could not contain its primary source edit.

**Why:** one shared checkout, no per-task branch, no locking — only the sibling's staging discipline separates the tasks.

**How to apply:**
- Stage explicitly by path, never `-A`/`-a`, so you never swallow a sibling's work.
- Commit each file as soon as its edit is complete and verified, rather than batching all edits until the end of the task.
- Before committing, check `git log -1 -- <your files>` / `git hash-object` against `HEAD:<path>`: an edit that shows no diff may already be inside a sibling's commit. Do not rewrite history to reclaim it — report the mix-up in the COMPLETED summary so the orchestrator can log the friction.
- Committing while a sibling's files sit staged in the shared index: use `git commit -m ... -- <your path>`. A pathspec commit takes only those paths' worktree content and never touches the rest of the index, so a sibling's staged-but-uncommitted work is neither swallowed nor unstaged. Confirmed 2026-09-08 in sprint 009. Every flag (`-m`, `-F <file>`) must come **before** the `--`, else git reads it as a pathspec and aborts. Lint the same way, scoped to your paths, so a sibling's staged files cannot fail your check.
- **Never stage-then-wait.** A sibling's plain `git commit` takes everything already staged, under its own `ASD-Task` trailer. Observed 2026-09-25 in sprint 018: Task 2 staged its files to lint them, and Task 3's commit swallowed them until it was soft-reset. Since sprint 019 the one-command form is the rule: `git-strategy.md` "Commit before review". `--only` alone rejects an untracked path ("pathspec did not match"), which is why the `git add` sits inside that same command.
- **Never `git stash` in this worktree.** It sweeps every uncommitted file — yours *and* every sibling's in-flight edit — into a stash they cannot see, and `--keep-index` does not protect anything you have not already staged. Observed 2026-09-07: a stash meant only to inspect HEAD state removed a whole task's work until popped. To compare against HEAD, read it directly (`git show HEAD:<path>`, `git diff HEAD -- <path>`) — never by moving the worktree.
- Related ledger hazard: [[sync-apply-ledger-gotcha]].
