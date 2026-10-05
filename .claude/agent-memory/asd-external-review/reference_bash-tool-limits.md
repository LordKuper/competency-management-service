---
name: bash-tool-command-length-limit
description: Bash tool rejects long commands (~>4.5 KB, incl. heredoc body) with a bogus "unexpected EOF while looking for matching quote" parse error before anything runs; only the fixed prompt stays in the heredoc, keep it small and have codex read the template and manifest by path; always merge stderr
metadata:
  type: reference
---

The Bash tool has a hard limit on total command text: a heredoc-bearing command of ~9 KB fails with `unexpected EOF while looking for matching ...` (reported line number varies) BEFORE anything executes; the same shape at ~3 KB passes; 9 KB of benign filler lines fails too, so it is size, not quoting. `run_in_background` and `dangerouslyDisableSandbox` change nothing. Threshold lies between ~3 KB and ~9 KB: stay at or under ~3 KB.

**How to apply:** only the fixed prompt stays in the heredoc now (the manifest and diff travel by path, never inline). Keep that prompt compact: "FIRST read the per-phase template under `.asd/templates/external-review/` and follow its rubric/verdict format exactly", slot values, the manifest path, scope note, output tail. Never inline the full template rubric. Bisect a failing command by piping the heredoc to `wc -c` instead of codex.

Always merge stderr with `2>&1` on the codex call: codex prints its quota error to stderr after echoing the payload, and `2>/dev/null` hides it (empty result). A quota error is not transient: one retry, then return `external review interrupted: quota exhausted` (never an availability skip after invocation; see `external-review.md` "Outcome contract"). See [[codex-invocation-mechanics]].
