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
- Large learn.microsoft.com pages (e.g. fundamentals/configuration) come back as a persisted tool-result file; Grep that file for the exact sentence instead of re-fetching.
- Mermaid C4 syntax (mermaid diagram_tool has no CLI to lint): grammar at `raw.githubusercontent.com/mermaid-js/mermaid/develop/packages/mermaid/src/diagrams/c4/parser/c4Diagram.jison` — unquoted alias token is `[^,]+`, so hyphenated registry ids work as aliases; element keywords include `Container_Boundary`, `ContainerDb`, `System_Ext`.
- TanStack Query docs: `tanstack.com/query/v5/docs/framework/react/guides/*` pages fetch fine; `…/docs/reference/*` (e.g. MutationCache) returned HTTP 500 on 2026-10-13, and raw `docs/reference/MutationCache.md` is 404 on `main` and on tag `v5.104.1`. Working: `…/docs/framework/react/reference/interfaces/MutationCacheConfig` (signatures) and raw `packages/query-core/src/mutation.ts` on `main` (callback order). `api.github.com/.../contents` returned 403 that day.
- OWASP cheat sheets (`cheatsheetseries.owasp.org/cheatsheets/*.html`) fetch fine; the redirect sheet does not cover `//` or backslash paths explicitly.
- When Bash is granted and docs pages omit a limit or edge behavior, grep the installed package under `web/node_modules/<pkg>/` (e.g. `dist/development/**`, `lib/**`): it is the pinned version, unlike docs sites that show `latest`. Docs can also promise less than the code does (antd Form.Provider: docs say named forms only) — record both and pin the relied-on part with a test.
- Transitive NuGet deps: nuspec groups list framework-inbox packages (e.g. `System.Formats.Asn1` for net10.0) that the .NET 10 lock files omit; state the lock-file graph, not the nuspec list, as what the project ships.

**Why:** WebFetch runs a small summarizer; on GitHub releases pages it rendered 2026 dates as 2024/2025 and invented plausible details. **How to apply:** take dates from CHANGELOG files / registry, cross-check any claim that matters with a second source, and never quote summarizer dates from releases pages.
