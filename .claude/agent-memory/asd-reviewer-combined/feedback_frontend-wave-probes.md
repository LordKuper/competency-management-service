---
name: frontend-wave-probes
description: how a 120-file web/ plus agent-memory impl-review wave was finished in about 45 turns - read ranges, one-Grep probes that found real defects, diff-to-file line arithmetic, antd d.ts deprecation check, re-review probes for extracted helpers, row-menu mutation and Russian plural probes
metadata:
  type: feedback
---

A wave of `web/**` (React 19, antd 6, TanStack Query) plus `.claude/agent-memory/**/*.md` read fine in 450-580 line chunks of
the diff (TSX tolerates the larger size); skip `package-lock.json`, `schema.d.ts`, OFL texts and binaries (type and size only).

**Probes that paid off, one Grep each:**
- `^\s+// ` over `web/src`: in-body comments hide inside object-literal arguments (`modal.confirm({ // note` ...) of arrow
  functions; code-style §7 counts them, a config-object comment in `vite.config.ts` is not in a function body.
- `export (const|function|type|interface|class) \w+` with `-o` over `web/src`, then one Grep per suspicious name: a theme
  `layout` constant with no importer, an `etag` field nobody reads, status-tag components called only with `isActive={false}`.
- `from "\.\./users/|from "\.\./org-structure/"` plus the helper name (`ifMatchOf`): generic helpers parked in one feature make
  features depend on each other both ways.
- `staleTime|refetchOnWindowFocus|gcTime` over `web/src`: no hit means the TanStack defaults (stale 0, refetch on focus)
  apply to lists of thousands of rows.
- `web/node_modules/antd/es/<component>/<Component>.d.ts` exists in this repo: grep it for `@deprecated` instead of trusting
  memory (antd 6 `Alert.message` is deprecated for `title`).
- A component that returns a bare element first and a wrapper (`Dropdown`) around the same element later remounts it:
  keyboard focus is lost; the CDP smokes in memory were mouse-only.
- Agent-memory files: grep each cited component or constant (`UserCard`, `SEARCH_TEXT_MAX_LENGTH`) in `web/src`, `src` and
  `tests`, and every "does not provide" / "NOT yet run" claim against the code; four stale claims turned up in one wave.
- A row-menu action with no confirm dialog (`useSendUserMail`, sprint 002): check for a pending guard or loading state, then
  the server handler (does it bump the version before a slow SMTP send, `Smtp:Timeout` 15 s?). A second click with the row's
  old `If-Match` gets 412 and `describeApiError` blames "другим пользователем".
- Russian text with a number from config (`Не короче ${n} символов`): one plural form is wrong for n ending in 1 (21, 31);
  `Intl.PluralRules("ru")` is the fix.

**Re-review of a review-fix delta (wave-3/iter-02, about 40 turns, one finding):**
- After a duplicated helper is hoisted into one shared module, Grep its call sites: a parameter every caller leaves at the
  default (`useDebouncedValue(value, delayMs = CONST)`, a test helper `json(body, status = 200)`) is a "premature config flag"
  and a pure alias of a stdlib call is "helper wrapping one stdlib call"; both are critical under the over-engineering row.
- Verify an antd/rc-dialog focus claim in `web/node_modules` before raising it: antd 6 has no `[tabindex="-1"]:focus` reset and
  the modal panel (`tabIndex -1`) is focused by script, so a global `:focus-visible` ring on it replaces the browser's own ring
  and is not a regression.
- `docs/ux/accessibility.html` declares focus order and management out of scope; a lost-focus-after-dialog note in memory is
  not a finding.
- Check each fix against the iter-01 report and `decisions-log.md` "review-fix" entry first; every iter-01 item was closed, so
  only the new surface (shared hooks, test infra) needed fresh scanning.

**Line arithmetic without a shell:** for a new-file hunk, file line = diff line minus the line number of its `@@` header; for a
whole-file replace, subtract the line number of the last `-` line instead.

**Language:** the dispatch header listed `language.docs: ru` and the policy puts reviews under `language.docs`, so the report
prose was Russian with the template headings and tokens in English; earlier waves of this sprint were English.

**Why:** the Read tool refuses over 25k tokens; these probes found the defects the diff read could not show.
**How to apply:** run them right after the diff read, before building the ledger; keep one finding per rule row.
