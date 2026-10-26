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

`git commit --only -- <paths>` commits the worktree content of those paths, never the index, so it cannot commit part of a file. When one file carries fixes for several findings, commit that file once with every fix in it, one `ASD-Task:` trailer per finding id, in the same one-command form (`git add -- <paths> && git diff --cached --check -- <paths> && git commit --only -F - -- <paths>`, `git reset -q -- <paths>` on failure). Hunk staging with `git apply --cached` plus a plain `git commit` breaks that form: the index stays shared between commands, and a plain commit takes whatever a sibling dispatch staged.
