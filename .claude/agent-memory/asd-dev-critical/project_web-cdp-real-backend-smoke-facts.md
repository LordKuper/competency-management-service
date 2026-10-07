---
name: web-cdp-real-backend-smoke-facts
description: Driving the real SPA through headless Chrome (CDP) against a scratch PostgreSQL and API - antd DOM ids, animation waits, real mouse events for selects, proxy and CSRF facts, shell quoting trap, harmless dev warnings
metadata:
  type: project
---

Method for a UI smoke of the whole stack (scratch `postgres:18.6-trixie` container on 127.0.0.1:35432, Release API on 127.0.0.1:35080 from `src/Competency.Api`, scratch Vite on 5900 with `API_PROXY_TARGET=http://127.0.0.1:35080`, Chrome driven from a node script). Facts that cost a failed run:

- **antd DOM ids**: `<Form name="login">` renders `<form id="login">` (there is no `name` attribute), fields get `id="login_email"`, `id="employee_fullName"`; a custom control only gets the id if it forwards it. Closed dropdowns stay in the DOM with `ant-dropdown-hidden`, so scope selectors with `.ant-dropdown:not(.ant-dropdown-hidden)`; a Select popup is `.ant-select-dropdown:not(.ant-select-dropdown-hidden)`, tree titles `.ant-select-tree-title`.
- **Menu and dropdown items move while the popup animates**: clicking right after the popup appears hits nothing; wait ~600 ms after opening, then click. A Select/TreeSelect only opens on real mouse events (`Input.dispatchMouseEvent` pressed and released at the element centre), not on `el.click()`; type the filter with `Input.insertText` after the click. To replace an input's value, `el.focus(); el.select()` then `Input.insertText`.
- **Sign-in lands on the first navigation item**, not the page asked for: an administrator ends on `/users`, so navigate to the target afterwards.
- **Proxy and CSRF**: through the Vite proxy the browser origin equals the Host the API sees (the proxy restores it), so mutations pass; a direct `curl -X POST` to the API needs `-H 'Origin: http://127.0.0.1:35080'`, and Cyrillic JSON goes through `--data-binary @file`.
- **Shell quoting trap**: a JS single quote inside a `node -e '...'` argument ends the shell string silently (`'"'` became `' + x + '`); build such text with `String.fromCharCode(34)` or write the script with the write tool. A `sed -i` or string `replace` that renames a port or id touches only the first match unless it is global.
- **Harmless dev noise**: the `rc-virtual-list` "scrollTo reach the max limitation" console error appears when a TreeSelect opens with a nested value selected (the shared `OrgUnitSelect`); each mounted `OrgUnitSelect` rereads the whole unit tree only when it is older than the 60 s list stale time, so a dialog opened right after the page load costs no extra tree request.
- `docker rm -fv <container>` frees the scratch container's anonymous volume; an anonymous volume that predates the run belongs to someone else, leave it.

**Why:** each was found by a failed or misleading scripted run.
**How to apply:** any later browser-level check of `web/` against the real API (see also [[web-org-card-hierarchy-facts]], [[scratch-process-control]], [[vs-debug-build-lock-and-release-config]], [[docker-spike-recipe]]).
