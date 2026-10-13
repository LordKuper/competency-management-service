---
name: feedback_promote-scripting
description: Method for bulk PRD promotion with scripts - scratchpad is shared with parallel promoters, and Bash heredoc/sed mangle backslashes
metadata:
  type: feedback
---

When promoting a large sprint PRD into persistent docs by script, use a private subfolder of the scratchpad (for example `scratchpad/ba/`) and write every script with the Write tool, not a Bash heredoc or `sed`.

**Why:** the scratchpad is shared with the parallel UX promoter, which writes its own files there; the Bash tool also drops backslashes from regex inside heredocs and `sed`, silently corrupting scripts (`\d` became `d`).

**How to apply:** parse sprint AC rows into JSON, build an old-to-new id map first (persistent docs use flat `AC-N`, new ids appended after the doc's last id, never reused), rewrite cross-references in one regex pass with placeholders for pre-built links, then check balance of tags, duplicate ids and cross-doc anchors before finishing. For Russian text, `python -I` ignores `PYTHONIOENCODING` and the Windows console garbles output: write script output to a UTF-8 file in the private subfolder and Read it. For terminology renames, list exact old/new phrases with expected counts and assert them (grammatical gender changes the adjectives and verbs around the noun). Bare group references such as `AC-29.` at a sentence end need a lookahead that allows a trailing period.
