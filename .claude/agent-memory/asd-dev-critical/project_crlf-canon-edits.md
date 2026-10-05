---
name: crlf-canon-edits
description: A canon file that is CRLF on disk (a checkout predating .gitattributes) breaks under a scripted edit anchored on an LF-only newline; the orphan CR makes git show the whole file rewritten
metadata:
  type: project
---

Anchor every scripted edit on the file's actual EOL, after reading its bytes. Root `.gitattributes` (`* text=auto eol=lf`) means a fresh checkout is LF, but a worktree checked out before that file was added is still CRLF on disk — check, never assume. On such a file a literal replacement whose search string *starts* with `\n` matches only the LF half of a `\r\n`, so deleting a whole line leaves a stray `\r` behind.

**Why:** git skips CRLF-to-LF normalization for a file holding a lone CR, so that one orphan byte turns a two-line edit into a whole-file rewrite in `git diff` and floods the whitespace lint with trailing-whitespace hits on untouched lines — hiding the real change from the reviewer and from the per-commit history the `pr` phase relies on.

**How to apply:**
- Read the bytes first; if the file holds CRLF, anchor the search string on `\r?\n` (or the full `\r\n`), never a bare `\n`; afterwards assert no lone CR remains.
- Sanity-check every scripted edit with `git diff --numstat` — a line count far larger than the edit means a line-ending problem, not a content problem.
- Lint staged content: `git diff --cached --check` must be clean before committing (`code-style.md` §19 — the unstaged form exits 0 once the damage is staged). A wall of "trailing whitespace" on untouched lines is this bug, not real whitespace.
- Writing a file with plain LF is harmless (git stores LF anyway) — only *mixed* endings inside one file break normalization.
- `sync.js --apply` (the orchestrator's wave sync, never the dev's) produces exactly that mix: it writes a managed block with LF and leaves the rest of a CRLF working-tree file as it was, so `AGENTS.md` can come back half-and-half. Before editing root `AGENTS.md`'s hand-written tail, check it for mixed endings; if it has them, rewrite the whole file to LF before staging.
- Escape sequences typed into a shell-fed Python one-liner do not survive intact here (a `\n` in the source arrived as a real newline and split a MEMORY.md index entry; `chr(92)` inside double quotes was eaten by the shell). Write the edit script to the scratchpad and run it by path, then re-read the diff.
- A scripted edit that matches nothing exits 0 and prints nothing — indistinguishable from success. A shell-fed `perl -0777 -pi -e` substitution over a multi-line quoted literal silently no-opped here. Count the occurrences of the search string first and `process.exit(1)` unless the count is exactly 1, then write; confirm with `git diff` in the same call.
- Related: [[sync-apply-ledger-gotcha]] for why the hash ledger stays stale after a canon edit until the wave sync.
