---
name: windows-scripted-edits
description: Scripted multi-file edits on this Windows checkout — Python text mode writes CRLF into LF files, and Cyrillic literals in a stdin heredoc can fail to match
metadata:
  type: feedback
---

When editing web sources with a Python heredoc, open with `newline=''` for both read and write; the default text mode writes CRLF into files the repo keeps as LF (`core.autocrlf=true`, `file` reports "with CRLF line terminators", git warns), and `sed -i 's/\r$//'` is the repair.

**Why:** a first scripted edit of `matrixApi.ts` turned the whole file CRLF; biome `ci` would then see a formatting diff.

**How to apply:** use the Edit tool for any replacement whose search string holds Cyrillic (one such `python -I -` assert failed to match text that was plainly in the file); use scripts only for ASCII anchors. After a batch of edits run `npx biome check --write src/features/<name>` from `web/` (import order, line width), then `npm run lint`, then `npm run build` and look for the "chunk larger than" warning.

Creating a large new `.tsx` file through a Bash `cat <<'EOF'` heredoc once failed with "unexpected EOF while looking for matching `''" (Cyrillic text with apostrophes and backticks); use the Write tool for whole new files, and run `npx biome check --write src/features` afterwards — it fixes line width in one pass.

The working tree is mixed: `git ls-files --eol` shows many `.cs` files `w/crlf` and others `w/lf` (index is LF for all). A multi-line anchor from a script never matches a CRLF file; read the file, normalise to `\n` for matching, and write back with the file's own EOL (a small `Doc` helper in the scratchpad). A script whose `str.replace` silently no-ops leaves the file unmodified — check `git status` after.

A Python edit opened without `encoding='utf-8'` reads and writes the cp1251 locale: new Cyrillic text typed in the script lands as cp1251 bytes inside a UTF-8 file, and the Write/Read tools then reject the file as non-UTF-8. Pass `encoding='utf-8'` both ways, or use Edit for any hunk with Cyrillic.

For a long multi-hunk patch, write the Python script to `.asd/tmp/*.py` with the Write tool and run it (`assert old in s` per hunk); the same heredoc failure hit a patch with `"""` strings, and a docstring-quote trick (`\"` before a closing `"""`) is fragile.
