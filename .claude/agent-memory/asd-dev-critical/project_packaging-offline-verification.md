---
name: packaging-offline-verification
description: How the Dockerfile and k8s manifests were checked without Docker or kubectl - fresh-clone publish, npm linux dry-run, yaml parse via root node_modules, why the Secret template sits outside deploy/k8s
metadata:
  type: project
---

Sprint 001 Task 10 (packaging) was verified offline on a machine with no Docker, kubectl or PostgreSQL:

- **Publish recipe the Dockerfile uses**: `ContinuousIntegrationBuild=true dotnet restore src/Competency.Api/Competency.Api.csproj` (locked mode comes from `Directory.Build.props`), then `dotnet publish ... -c Release -o <dir> --no-restore -p:UseAppHost=false -p:OpenApiGenerateDocumentsOnBuild=false`. The last property stops only the build hook that starts the app and writes `../../openapi`; the committed contract stays. `deps.json` and the file list carry no EF Design, Roslyn or ApiDescription.Server. `wwwroot/.gitkeep` is published, so the Dockerfile removes `wwwroot` and takes it from the SPA stage.
- **Fresh-context simulation**: `git archive HEAD global.json Directory.Build.props src | tar -x -C <scratch>` (and `git archive HEAD web` for the SPA) reproduces what a Docker COPY sees, without host `obj`/`bin`; compare `packages.lock.json` with `cmp` afterwards. Web: `npm ci` ~12 s, `npm run build` ~30 s (it runs the typecheck first).
- **Linux optional binaries without Docker**: `npm ci --os=linux --cpu=x64 --libc=glibc --dry-run --loglevel=silly | grep linux-x64` lists the planned adds (rolldown, lightningcss, biome) from the lock alone. A real install, and a lock regeneration if one were needed, still wants a Linux container.
- **Env var mapping**: the published dll started with `ConnectionStrings__Default` at a closed port exits at `MigrateAsync` before it listens, and the password is not in the log. The Deployment relies on the restart loop plus a `startupProbe` for that.
- **Manifest checks**: root `node_modules` holds the `yaml` package (design.md tooling); `createRequire("<repo>/package.json")("yaml")` parses every document and a short script cross-checks selectors, named ports, mounts, ConfigMap and Secret keys. kubectl `--dry-run=client` needs an API server, so it stays a Manual verification line.
- **Secret template placement**: `deploy/secret.template.yaml` sits above `deploy/k8s/` on purpose. `kubectl apply -f deploy/k8s/` applies every yaml in the directory, and a Secret with placeholders would put a placeholder password into the PostgreSQL data directory on first init, where a later Secret change cannot reach it.

**Why:** each of these answered "how do we know the packaging works with no Docker"; the Docker run itself was deferred to MS-1 and is now done (see [[docker-spike-recipe]]; npm linux install needed no lock regeneration, tag and digest builds give byte-identical /app).
**How to apply:** re-verifying or changing the Dockerfile, `deploy/**` or the publish flags; the offline checks are still the quick pre-Docker pass.
