---
name: temp-repo-git-determinism
description: tests/run.js temp-git-repo tests pin their own git calls with -c flags, but git spawned inside runtime.js inherits host global/system config (diff.noprefix etc.); also impl-review Testing's manifest holds only isTest files + test-plan paths (since sprint 015), per review wave (since sprint 017)
metadata:
  type: feedback
---

Sprint 015 added a temp-repo `emit-manifest --base/--head` test. The test's own `git(...)` helper passes `-c user.name/... core.autocrlf=false`, but the git that `runtime.js` spawns (`rangeRenames`, `writePatch`) runs with no `-c` and no env isolation, so it reads the developer's global config. The test parses `diff --git a/X b/Y` headers, so `diff.noprefix=true` on a host breaks it with no code change.

**Why:** the -c flags on the fixture helper make the test look hermetic; the subprocess under test is where host config leaks in.

**How to apply:** for any test that runs a runtime subcommand calling git, check whether the child env sets `GIT_CONFIG_NOSYSTEM`/`GIT_CONFIG_GLOBAL` or production pins prefixes.

Scope note: since sprint 015 `reviewerFiles` narrows impl-review Testing to `isTest` scope files plus `--test-plan` paths. Source files are context only; judge their coverage by reading them, but findings attach only to the manifest's files. Since sprint 017 a large scope is divided into sequential review waves, each one standard review (iteration id `wave-<K>/iter-NN`, `sprint-lifecycle.md` "Review iteration counters"): one manifest and one `<fingerprint>.diff` per reviewer per iteration, no other division. Testing's list in wave K is that wave's `isTest` files plus `test-plan.md` and its segments, so a wave may hold no test file at all. Resolve every rubric row from that list anyway: read the tests covering the wave's source files as context, and attach a coverage gap to the `test-plan.md` (or segment) row whose decision is missing or false. A defect inside a test file is a finding only in the wave whose list holds that file; a closed wave is never reopened — a later change to its file is reviewed in the current wave. Use `n/a` only on a predicate the manifest authorizes for that exact id, verified true. See [[no-shell-review-method]].
