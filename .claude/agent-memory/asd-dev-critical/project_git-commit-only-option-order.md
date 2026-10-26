---
name: git-commit-only-option-order
description: git commit --only -F - -- <paths> needs every option before the -- separator; a -F - placed after the pathspec is read as a path, not the message source
metadata:
  type: project
---

In `git commit --only -F - -- <paths>`, every option (`--only`, `-F -`, `-m`, …) must come before `--`. Anything after `--` is pathspec, so `git commit --only -- <paths> -F -` treats `-F` and `-` as paths: the commit fails on an unmatched pathspec or ignores the piped message.

**Why:** memory-fix and new-file commits here run as one compound `git add -- <paths> && git commit --only …` command (see [[parallel-agent-commit-sweep]]), with the message piped via heredoc into `-F -`; misplacing `-F -` breaks that one-shot commit.

**How to apply:** write it as `git commit --only -F - -- <paths> <<'EOF' … EOF`, options first, `--` last before paths.

A file already removed with `git rm` is no longer a valid `git add -- <path>` pathspec: one such path makes the whole `git add` fail and stage nothing. Leave it out of `git add`, but list it after `--` in `git commit --only` (HEAD knows it, so the deletion is committed).

`git commit --only -- <paths>` commits the worktree content of those paths, never the index, so it cannot commit part of a file. When one file carries fixes for several findings (one commit each), stage the chosen hunks with `git apply --cached --recount -` fed from `git diff -U1 -- <file>` (header + picked `@@` hunks; re-list hunks after each commit, indices shift), then a plain `git commit -F -` with nothing else in the index.
