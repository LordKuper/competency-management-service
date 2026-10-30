# syntax=docker/dockerfile:1
# check=skip=FromPlatformFlagConstDisallowed
# The skipped check flags the constant --platform below, which is deliberate: the delivered image is linux/amd64
# whatever the architecture of the build host.

# Base images arrive through build arguments so a digest can be injected without editing this file:
#   --build-arg NODE_IMAGE=node:24.21.0-trixie-slim@sha256:<DIGEST>
# The defaults are tags and only fit local experiments; deploy/README.md describes the pinned build.
ARG NODE_IMAGE=node:24.21.0-trixie-slim
ARG SDK_IMAGE=mcr.microsoft.com/dotnet/sdk:10.0.401
ARG RUNTIME_IMAGE=mcr.microsoft.com/dotnet/aspnet:10.0.12-noble

# The client types in web/src/api/schema.d.ts are committed, so this stage does not need the backend.
FROM --platform=linux/amd64 ${NODE_IMAGE} AS spa
WORKDIR /src/web
COPY web/package.json web/package-lock.json web/.npmrc ./
RUN npm ci
COPY web/ ./
RUN npm run build

FROM --platform=linux/amd64 ${SDK_IMAGE} AS publish
# Locked-mode restore comes from Directory.Build.props when ContinuousIntegrationBuild is true.
ENV ContinuousIntegrationBuild=true \
    DOTNET_CLI_TELEMETRY_OPTOUT=1
WORKDIR /src
COPY global.json Directory.Build.props ./
COPY src/Kalibr.Api/Kalibr.Api.csproj src/Kalibr.Api/packages.lock.json src/Kalibr.Api/
COPY src/Kalibr.Platform/Kalibr.Platform.csproj src/Kalibr.Platform/packages.lock.json src/Kalibr.Platform/
COPY src/Kalibr.Audit/Kalibr.Audit.csproj src/Kalibr.Audit/packages.lock.json src/Kalibr.Audit/
COPY src/Kalibr.OrgStructure/Kalibr.OrgStructure.csproj src/Kalibr.OrgStructure/packages.lock.json src/Kalibr.OrgStructure/
COPY src/Kalibr.UserManagement/Kalibr.UserManagement.csproj src/Kalibr.UserManagement/packages.lock.json src/Kalibr.UserManagement/
COPY src/Kalibr.CareerFramework/Kalibr.CareerFramework.csproj src/Kalibr.CareerFramework/packages.lock.json src/Kalibr.CareerFramework/
COPY src/Kalibr.Promotion/Kalibr.Promotion.csproj src/Kalibr.Promotion/packages.lock.json src/Kalibr.Promotion/
COPY src/Kalibr.Desk/Kalibr.Desk.csproj src/Kalibr.Desk/packages.lock.json src/Kalibr.Desk/
RUN dotnet restore src/Kalibr.Api/Kalibr.Api.csproj
COPY src/ src/
# openapi.json is a committed contract, so the build-time document generation (it starts the app) stays off.
# EntityFrameworkCore.Design and ApiDescription.Server are PrivateAssets="all" and do not reach the output.
# wwwroot is dropped because the SPA stage is its only source in the image.
RUN dotnet publish src/Kalibr.Api/Kalibr.Api.csproj -c Release -o /app --no-restore \
        -p:UseAppHost=false -p:OpenApiGenerateDocumentsOnBuild=false \
    && rm -rf /app/wwwroot

FROM --platform=linux/amd64 ${RUNTIME_IMAGE} AS final
WORKDIR /app
COPY --from=publish /app ./
COPY --from=spa /src/web/dist ./wwwroot
# Fallback for a run without the PVC; the Deployment mounts a volume at the same path.
ENV ASPNETCORE_HTTP_PORTS=8080 \
    DataProtection__KeysPath=/var/lib/kalibr/keys
RUN mkdir -p "$DataProtection__KeysPath" && chown "$APP_UID:$APP_UID" "$DataProtection__KeysPath"
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "Kalibr.Api.dll"]
