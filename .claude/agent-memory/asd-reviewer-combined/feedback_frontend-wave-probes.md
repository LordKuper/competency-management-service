---
name: frontend-wave-probes
description: how a 100-120-file web/ plus agent-memory impl-review wave was finished in about 45-60 turns - read ranges, one-Grep probes that found real defects (unused exports and props, secure-context-only browser APIs, raw px and disabled-without-reason against design-system rules, Russian plurals, per-id name reads and client-side exclude in server-search pickers), diff-to-file line arithmetic, antd d.ts deprecation check, re-review probes for extracted helpers, JSDoc detached by a new const
metadata:
  type: feedback
---

A wave of `web/**` (React 19, antd 6, TanStack Query) plus `.claude/agent-memory/**/*.md` read fine in 450-580 line chunks of
the diff (TSX tolerates the larger size); skip `package-lock.json`, `schema.d.ts`, OFL texts and binaries (type and size only).
Grep `^diff --git` over the diff first: its line numbers give every file's range, so whole-file tests can be skimmed or skipped.

**Probes that paid off, one Grep each:**
- `^\s+// ` over `web/src`: in-body comments hide inside object-literal arguments (`modal.confirm({ // note` ...) of arrow
  functions, inside JSX prop lists and inside `it(...)` callbacks of tests; code-style §7 counts them, a config-object comment
  in `vite.config.ts` is not in a function body.
- `^export (const|function|type|interface|class|async function) \w+` with `-o`, `path` per feature folder (a brace list in
  `glob` returns nothing), then one Grep per suspicious name: a theme `layout` constant with no importer, an `etag` field
  nobody reads, type aliases (`XRights`, `XDiff`, `XRoles`) declared and never imported.
- Props the caller passes but the component never reads: a wrapper that spreads `{...props}` into a body which destructures
  only some keys hides them; Grep the prop name inside the component file. Also an optional prop no caller ever passes
  (`trackId?`) leaves its branch dead.
- `randomUUID|navigator\.clipboard|showOpenFilePicker` over `web/src`: these exist only in a secure context (HTTPS or
  localhost). When `deploy/README.md` leaves TLS open and the cookie policy is `SameAsRequest`, plain HTTP is a supported mode
  and the feature breaks there; manual checks on localhost cannot see it. `crypto.getRandomValues` works without one.
- `design-system.md` §6 and §7 under UI conformance: Grep `padding: \d|width: \d|_WIDTH = \d` and `font-family` in TSX/CSS
  (raw px is a high finding even when the width itself was accepted), and every `disabled={` that is not a pending flag:
  a persistent disabled state needs visible reason text.
- `from "\.\./users/|from "\.\./org-structure/"` plus the helper name (`ifMatchOf`): generic helpers parked in one feature make
  features depend on each other both ways; check decisions-log first, the orchestrator may have accepted it ("пока в career").
- `staleTime|refetchOnWindowFocus|gcTime` over `web/src`: no hit means the TanStack defaults (stale 0, refetch on focus)
  apply to lists of thousands of rows.
- `useQueries` fed by the ids of a table page (a `useEntryNames`-style hook resolving column names one `GET /{id}` each): an
  N+1 on the client, repeated on every focus with default staleness. Compare with sibling responses that already carry the
  names (`ownerOrgUnitName`, `directionName`); the fix is an additive name field, so raise it with a `question:` escalation.
- A server-search picker with a fixed page size (20) that drops "already used" ids on the client (`exclude`): the visible list
  shrinks or goes empty while free entries sit on the next pages. The page size has to grow by the excluded count.
- `web/node_modules/antd/es/<component>/index.d.ts` exists in this repo: grep it for `@deprecated` instead of trusting
  memory (antd 6 `Alert.message` is deprecated for `title`, `Steps` `description` for `content`, `direction` for `orientation`;
  Select's top-level search props are in `@rc-component/select/es/Select.d.ts`, moved into `showSearch`).
- A component that returns a bare element first and a wrapper (`Dropdown`) around the same element later remounts it:
  keyboard focus is lost; the CDP smokes in memory were mouse-only.
- Agent-memory files: grep each cited component or constant (`UserCard`, `SEARCH_TEXT_MAX_LENGTH`) in `web/src`, `src` and
  `tests`, and every "does not provide" / "NOT yet run" claim against the code; four stale claims turned up in one wave.
- A row-menu action with no confirm dialog (`useSendUserMail`): check for a pending guard or loading state, then
  the server handler (does it bump the version before a slow SMTP send, `Smtp:Timeout` 15 s?). A second click with the row's
  old `If-Match` gets 412 and `describeApiError` blames "другим пользователем".
- A write helper that GETs the record and sends its fresh tag at once (delete or approve from a list row whose response has no
  version) makes `If-Match` unable to ever fire against what the user saw.
- Russian text with a number (`Не короче ${n} символов`, «и N других связанных записей»): a `count === 1` special case or one
  fixed form is wrong for n ending in 1 (21, 31); `Intl.PluralRules("ru")` is the fix.
- A review-fix that adds a module-level `const` to a hook file: check it did not land between the exported function and its
  JSDoc (`SENDING_KEY`); the doc then attaches to the const and the export has none.

**Re-review of a review-fix delta (about 40 turns, one finding):**
- After a duplicated helper is hoisted into one shared module, Grep its call sites: a parameter every caller leaves at the
  default (`useDebouncedValue(value, delayMs = CONST)`, a test helper `json(body, status = 200)`) is a "premature config flag"
  and a pure alias of a stdlib call is "helper wrapping one stdlib call"; both are critical under the over-engineering row.
- Verify an antd/rc-dialog focus claim in `web/node_modules` before raising it: antd 6 has no `[tabindex="-1"]:focus` reset and
  the modal panel (`tabIndex -1`) is focused by script, so a global `:focus-visible` ring on it replaces the browser's own ring
  and is not a regression.
- `docs/ux/accessibility.html` declares focus order and management out of scope; a lost-focus-after-dialog note in memory is
  not a finding.
- Check each fix against the `decisions-log.md` "review-fix" entry and the code only; never open another iteration's review
  files (`review-policy.md` "Clean-context review iteration" forbids it). Then scan only the new surface (shared hooks, test infra).
- The plan's UI-text paragraph approved for the review-fix lists every new string; Grep it once and compare instead of
  reading the whole «Тексты интерфейса» section. The tester's "Added tests" rows name equivalent mutants (e.g. a hand-made
  id de-duplication that react-query already does): read them before raising a redundancy.

**Line arithmetic without a shell:** for a new-file hunk, file line = diff line minus the line number of its `@@` header; for a
whole-file replace, subtract the line number of the last `-` line instead. Grep the current file for the line instead when cheap.

**Language:** the dispatch header listed `language.docs: ru` and the policy puts reviews under `language.docs`, so the report
prose was Russian with the template headings and tokens in English; earlier waves of this sprint were English.

**Writing memory:** `Write` replaces the whole file; never write a partial file. If one was lost, the full text of a reviewer
memory file is in the archived sprint review diffs (`.asd/sprints/archived/**/*.diff`, new-file hunks). Write no sprint
number, wave or iteration number into memory: `runtime.js` `MEMORY_HISTORY_PATTERNS` rejects them at the memory commit.

**Why:** the Read tool refuses over 25k tokens; these probes found the defects the diff read could not show.
**How to apply:** run them right after the diff read, before building the ledger; keep one finding per rule row.
