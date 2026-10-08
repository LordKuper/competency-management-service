---
name: techref-verification-sources
description: How to verify npm/NuGet versions for tech-reference docs with WebFetch only (no Bash), and which summarizer quirks to cross-check
metadata:
  type: reference
---

For tech-reference verification (Phase 5-6) the architect has no Bash; use WebFetch on registry endpoints:
- `https://registry.npmjs.org/<pkg>/<version>` — manifest (engines, peerDependencies, optionalDependencies, scripts, os/cpu/libc); `https://registry.npmjs.org/-/package/<pkg>/dist-tags` — latest/next tags (scoped names work unescaped).
- NuGet: `https://api.nuget.org/v3-flatcontainer/<id-lowercase>/index.json` and `/<ver>/<id>.nuspec`.
- Changelogs: prefer `raw.githubusercontent.com/<org>/<repo>/<branch>/.../CHANGELOG.md`; github.com `blob/` pages return only UI chrome.
- NuGet license when the nuspec says `<license type="file">`: `https://www.nuget.org/packages/<Id>/<ver>/License` returns the license text (summarized by the fetch tool — say so in the doc); the package page itself shows only a link. MSBuild SDK packages (`packageTypes` MSBuildSdk) can be proprietary Microsoft terms, not MIT.
- Bundled npm version of a Node release: `raw.githubusercontent.com/nodejs/node/v<ver>/deps/npm/package.json` (release blog omits it).

- Highest version on a semver line (e.g. `@types/node` 24.x when `latest` is another major): `registry.npmjs.org/<pkg>/<range>` returns 404; use `https://data.jsdelivr.com/v1/packages/npm/<pkg>/resolved?specifier=24`, then confirm with the exact-version manifest and 404 probes for the next patch/minor.
- Docker image versions/dates: `https://hub.docker.com/v2/repositories/<ns>/<repo>/tags?page_size=8&ordering=last_updated` returns exact ISO timestamps per tag (reliable, unlike GitHub releases pages); pair with raw CHANGELOG/LICENSE from `raw.githubusercontent.com`.
- Docker image config without running it: `https://hub.docker.com/v2/repositories/<ns>/<repo>/tags/<tag>/images` lists per-platform digest, size and layer instructions (USER/VOLUME/HEALTHCHECK/EXPOSE presence, base rootfs).
- GitHub JSON API works via WebFetch: `api.github.com/repos/<o>/<r>/releases/tags/<tag>` (exact published_at), `api.github.com/repos/<o>/<r>/contents/<dir>?ref=<tag>` (find which source file holds a function before fetching raw).
- unpkg `index.d.ts` / `package.json` fetches work for `@types/*` (large files come back partially summarized — say what was not seen).

**Why:** WebFetch runs a small summarizer; on GitHub releases pages it rendered 2026 dates as 2024/2025 and invented plausible details. **How to apply:** take dates from CHANGELOG files / registry, cross-check any claim that matters with a second source, and never quote summarizer dates from releases pages.
