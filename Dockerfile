# Builds the Vue app and the API into one image; used by docker-compose.yml.
# (api/GameEventApi/Dockerfile is Visual Studio's API-only container profile.)

FROM node:24-alpine AS web
WORKDIR /web
COPY web/package.json web/package-lock.json ./
RUN npm ci
COPY web/ ./
RUN npm run type-check && npx vite build --outDir dist

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY api/GameEventApi/GameEventApi.csproj api/GameEventApi/
RUN dotnet restore api/GameEventApi/GameEventApi.csproj
COPY api/GameEventApi/ api/GameEventApi/
RUN dotnet publish api/GameEventApi/GameEventApi.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .
COPY --from=web /web/dist ./wwwroot
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "GameEventApi.dll"]
