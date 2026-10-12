---
name: scripted-insert-doc-orphan
description: Inserting new members via a script anchored on a method signature lands them between that method and its XML summary, which then documents the wrong member; the build does not catch it
metadata:
  type: project
---

A `node -e` replace that inserts new private members before `    private async Task<...> SomeMethodAsync(` puts them under the summary of `SomeMethodAsync`: the doc comment now sits on the first inserted member and the original method has none. A private member needs no doc, so the build stays at 0 warnings and nothing flags it.

**Why:** happened once in a review-fix round; caught only by reading the combined diff before reporting.
**How to apply:** anchor scripted insertions on the closing `}` of the previous member or on the `/// <summary>` line of the next one, never on a signature line; read `git diff` of every scripted edit. See also [[crlf-canon-edits]], [[heredoc-backslash-collapse]].
