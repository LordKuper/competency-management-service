---
name: git-commit-only-option-order
description: git commit --only -F - -- <paths> needs every option before the -- separator; a -F - placed after the pathspec is read as a path, not the message source
metadata:
  type: project
---

In `git commit --only -F - -- <paths>`, every option (`--only`, `-F -`, `-m`, …) must come before `--`. Anything after `--` is pathspec, so `git commit --only -- <paths> -F -` treats `-F` and `-` as paths: the commit fails on an unmatched pathspec or ignores the piped message.

**Why:** memory-fix and new-file commits here run as one compound `git add -- <paths> && git commit --only …` command (see [[parallel-agent-commit-sweep]]), with the message piped via heredoc into `-F -`; misplacing `-F -` breaks that one-shot commit.

**How to apply:** write it as `git commit --only -F - -- <paths> <<'EOF' … EOF`, options first, `--` last before paths.
