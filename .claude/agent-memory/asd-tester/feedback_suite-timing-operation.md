---
name: suite-timing-operation
description: Open a timing `suite` operation before every suite run (also the run before any test is written) and close it right after, in separate calls
metadata:
  type: feedback
---

Open a `suite` operation before every suite run, including the run before any test is written: `node .asd/runtime.js timing … --open "suite <phase>#<n>" --kind suite --parent <dispatch id>`. Close it right after the run. Open and close are separate calls, never chained with the test command.

**Why:** chained with the test command, the close is lost when `tail` or a failing command ends the chain, and the log keeps an open operation.

**How to apply:** one open call, then the test command alone, then one close call, whatever the exit code was.
