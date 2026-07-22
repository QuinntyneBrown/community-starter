# syntax=docker/dockerfile:1.7

FROM node:24.18.0-alpine AS web-build
WORKDIR /src
ENV ASTRO_TELEMETRY_DISABLED=1
COPY package.json package-lock.json ./
COPY frontend/package.json frontend/package.json
COPY marketing/package.json marketing/package.json
RUN npm install --global npm@11.6.2 && npm ci
COPY design-system design-system
COPY frontend frontend
COPY marketing marketing
RUN npm run build

FROM mcr.microsoft.com/dotnet/sdk:10.0.101 AS api-build
WORKDIR /src
COPY global.json Directory.Build.props Directory.Packages.props .editorconfig ./
COPY backend backend
RUN dotnet restore backend/CommunityStarter.sln
RUN dotnet publish backend/src/CommunityStarter.Api/CommunityStarter.Api.csproj \
    --configuration Release --no-restore --output /out

FROM mcr.microsoft.com/dotnet/aspnet:10.0.1 AS final
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080 \
    DOTNET_EnableDiagnostics=0
COPY --from=api-build --chown=1654:1654 /out ./
COPY --from=web-build --chown=1654:1654 /src/marketing/dist ./wwwroot
COPY --from=web-build --chown=1654:1654 /src/frontend/dist/frontend/browser ./wwwroot/app
USER 1654
EXPOSE 8080
ENTRYPOINT ["dotnet", "CommunityStarter.Api.dll"]
