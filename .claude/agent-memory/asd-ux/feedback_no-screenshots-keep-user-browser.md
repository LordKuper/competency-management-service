---
name: feedback-no-screenshots-keep-user-browser
description: Permanent user instruction (2026-10-14) - never render UI screenshots or headless browser runs; never close or kill a browser the user opened
metadata:
  type: feedback
---

- No UI screenshots and no headless renders of any kind (no `chrome.exe --headless`, no `--screenshot`, no `--dump-dom` of mockups). Verify mockups by reading the HTML and by tag-balance scripts only.
- Never close or kill a browser the user opened: no `taskkill` or `Stop-Process` on chrome/msedge, no closing of their tabs or windows.

**Why:** the user gave this as a permanent instruction after sprint 004 Task 17, when headless Chrome renders had been used for concept checks.
**How to apply:** any time a mockup or design-system page needs a visual check, skip the render, say it was not rendered, and rely on structure checks. Supersedes the earlier habit of headless-Chrome screenshots noted in [[project-ux-docs-conventions]].
