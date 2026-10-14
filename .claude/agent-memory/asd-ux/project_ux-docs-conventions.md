---
name: project-ux-docs-conventions
description: Conventions and traps for persistent docs/ux/*.html in this repo (a11y scope, mockup style, tokens, items to re-verify in code)
metadata:
  type: project
---

- `docs/ux/accessibility.html` declares keyboard behaviour, ARIA and screen readers OUT of scope by customer decision (2026-10-05). Any focus/keyboard rule is a scope extension: return proposed text in Flagged choices, do not edit (asd-ux must not write a11y rules). User decision 2026-10-12: do not extend accessibility.html; focus and keyboard-drag rules live only in the screen UX docs, with no links to accessibility.html.
- Persistent ux docs wrap content in `<div class="ds">` with `--ds-*` token variables (hex only at that root, primitive layer); mockups reference `var(--ds-*)` only. org-structure.html uses inline styles plus a scoped `o-*` style block; career-framework.html uses a scoped class set. No sprint/task ids; no AC numbers for new subsystems.
- Resolved in code and docs (2026-10-12): dark Menu popup uses `surface-inverse` (`darkPopupBg`, `darkSubMenuItemBg`); catalogue row actions show at most two links, the rest in a "…" Dropdown (`RowActions.tsx`); scales and nested tracks have their own empty texts. Still check when touching: the Menu popup tokens in `web/src/app/theme.ts`, and that new tables use `RowActions` rather than inline link lists.

**Why:** found during a design-promote; they recur on any later UX promote.
**How to apply:** check before writing a11y text, new mockups, or reviewing shell/menu visuals; then update `career-framework.html` if the code changed.

- Sprint 004 Task 14 (2026-10-13): `desk.html` has section `#compact-rules` (К1–К6 compactness, В1–В4 return, Л1–Л5 step ruler); scenario mockups have no h1 (crumb only), block titles `.h4`, ruler `.sth` one line with "Шаг n из N", flat `.bar`. «Направление» → «Область» renamed only in `career-framework.html` and `desk.html` (app-shell, user-management, DESIGN.md had none). Big multi-block edits: node script with line anchors + `expect` checks, applied bottom-up; verify with `check.js`-style tag balance only (headless Chrome screenshots are forbidden since 2026-10-14, see [[feedback-no-screenshots-keep-user-browser]]). `sed -i` strips CRs again (desk.html is CRLF in worktree, LF in index; career-framework is LF).
- Sprint 004 promote (2026-10-13): `docs/ux/desk.html` is the desk subsystem doc (scenario screens, desk, letters, states; no AC/U ids, no coverage table); header/counter/login-return live in `app-shell.html` (ids `m-shell`, `m-counter`, `m-login`); matrix bar, track window, publish-confirm window in `career-framework.html` (ids `m-matrix`, `m-track-form`, `m-publish-confirm`). Every doc with `.mockup-preview` carries a dark-scheme rule keeping the preview surface light; `.ds` root must never set app font/color. Working-tree EOL is mixed per file (career-framework/user-management LF, others CRLF): preserve each file's EOL; bash heredocs here halve backslashes, so write node scripts with the Write tool; `sed -i` can strip CRs.
