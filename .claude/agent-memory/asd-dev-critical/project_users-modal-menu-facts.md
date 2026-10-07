---
name: users-modal-menu-facts
description: Task 19 (users row menu + create/edit modal) - why the 409 alert is suppressed for field conflicts, reset target looked up from the live list, gcTime 0 on the account query, and the CDP smoke facts
metadata:
  type: project
---

Task 19 (2026-10-07), `web/src/features/users`, antd 6.6.5 / TanStack Query 5.104.1, scratch DB + Release API + Vite 5900, all removed:

- **409 has two shapes**: a taken e-mail comes as `HttpValidationProblemDetails` (`errors.email` plus `title`/`detail` with the same text), every other 409 (binding `AlreadyBound`, last administrator) has no `errors`. `showFieldErrors` puts the first under E-mail, `ErrorAlert` would print it again (the Task 16 "shown twice" note), so `UserModal` hides the alert for `status === 409 && errors !== undefined`. A 400 keeps its generic alert ("исправьте отмеченные поля"), it does not repeat the field text. Counting the Russian sentence in the modal text (exactly 1) is the check.
- **Reset-password target is looked up by id** from the live list data (`dialog.kind === "resetPassword"` + `data.items.find`), not stored as a row copy: the form submits `If-Match` from the row, and after a 412 the invalidation refetches the list, so a retry carries the new version. `ResetPasswordModal` lost its `open` prop (mounted only while shown, like `EmployeeModal`).
- **`userQuery` has `gcTime: 0`**: with the default cache the edit modal would first show the previous (older-version) detail of that account, e.g. right after the same admin blocked it from the menu, and a fast Save would 412 against the admin's own change.
- Save invalidates `usersQueryKey` and `invalidateOrgStructure` in `onSettled`, because employee cards show the account e-mail; inactive queries are only marked stale. Proven with SPA navigation (menu click, no reload): after binding, opening Оргструктура refetched `GET /employees?orgUnitId=...` and the card showed the new e-mail.
- Blocking the last administrator from the row menu still surfaces as the `message.error` toast of `useBlockUser` (the confirm closes); only the edit modal shows the 409 inline.
- **Smoke harness**: the Task 18 scripts adapt with `sed` (DB name, port, key dir). Facts new here: `.ant-form-item:has(#user_employeeId)` works in Chrome selectors; the select clear icon needs the real `mouseMoved` that `clickJs` sends first; EmployeePicker options need ~1.2 s after `Input.insertText`; the password-reset login check runs from node `fetch` with `Origin` set to the API origin (a browser fetch would replace the admin cookie); open the menu by `button[aria-label="Действия с пользователем <email>"]`, wait ~800 ms, items under `.ant-dropdown:not(.ant-dropdown-hidden)`. The whole 60-check run took about 90 s and passed first time.

**Why:** each decided the modal's error handling or saved a failed smoke run.
**How to apply:** any later change to the account screens, the 409/412 handling in modals, or a browser smoke of `/users` (see also [[web-cdp-real-backend-smoke-facts]], [[web-org-card-hierarchy-facts]], [[employee-dismissal-cascade-facts]]).
