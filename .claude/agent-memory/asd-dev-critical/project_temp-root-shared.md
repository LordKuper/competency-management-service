---
name: temp-root-shared
description: The Windows TEMP root here holds Visual Studio logs and earlier agents' files; a glob move out of it takes them along
metadata:
  type: project
---

`$TEMP` (`C:\Users\Michieru\AppData\Local\Temp`) holds Visual Studio setup and JS debugger logs and loose files of earlier agents. On 2026-10-13 a `mv "$TEMP"/*.txt <scratchpad>` meant for 7 own files moved about 30 foreign ones, which then had to be moved back one by one.

**Why:** a glob in a shared directory cannot tell your files from others'.
**How to apply:** write temporary files only to the session scratchpad, and move or delete only by exact name, never by glob in `$TEMP`.
