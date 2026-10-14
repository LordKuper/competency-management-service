---
name: no-screenshots-keep-user-browser
description: Permanent user rule - no UI screenshots or headless renders; never close or kill a browser the user opened.
metadata:
  type: feedback
---

- No UI screenshots and no headless renders at all, for any verification.
- Never close or kill a browser the user opened: no taskkill or Stop-Process on chrome/msedge, no closing their tabs or windows.

**Why:** user instruction (permanent). Earlier headless runs ended with `taskkill /F /IM chrome.exe`, which kills every chrome process, the user's included.

**How to apply:** verify UI by lint, build and reading code only; report the visual check as not done. This overrides coordinator requests to verify visually.
