---
name: web-anonymous-link-screens-facts
description: Sprint 002 Task 6 web facts - token from the URL fragment under StrictMode, router links with antd link styling, empty 202/204 in openapi-fetch, mail-sent reporting
metadata:
  type: project
---

- `main.tsx` renders under `StrictMode`, which calls `useState` initializers twice. Reading the `#token=` fragment must stay pure in the initializer (`useState(() => new URLSearchParams(hash.slice(1)).get("token"))`); the fragment is dropped in a `useEffect` with the router's `navigate(pathname, { replace: true })`, which is `history.replaceState` and keeps `router.state.location` free of the token too. Clearing inside the initializer would lose the token on the second call.
- antd has no global `a` styles (the `App` component sets only colour/font); link styling lives on `.ant-typography-link`. A router link that looks like antd is `Typography.Link` with `href={useHref(to)}` and `onClick={useLinkClickHandler<HTMLElement>(to)}` (handles modifier clicks) - `AuthLink` in `features/auth/AuthCard.tsx`.
- `openapi-fetch` 0.17 returns `data: undefined` for 204 and for `Content-Length: 0`, and for a chunked/no-length OK body it reads text and parses only if non-empty, so the empty 202 of `forgot-password` never throws in `unwrap`.
- The invalid-link answer is a 400 ProblemDetails without `errors` (title/detail in Russian); a password-policy refusal is a 400 with `errors.password`. `describeApiError` turns any 400 without `errors[""]` into the generic "исправьте отмеченные поля", so the link screens read `title`/`detail` themselves.
- `mailSent === false` is shown with `modal.warning` (stays until closed), success with a toast; a mutation `onSuccess` must not return the toast thenable (it would hold the mutation pending for the toast's lifetime, see `useBlockUser`).

**Why:** each decided how the anonymous screens and the mail actions were written.
**How to apply:** later anonymous screens, link/token handling in the SPA, and impl-test of these screens (see also [[web-ui-test-and-form-gotchas]], [[users-modal-menu-facts]]).
