FROM mcr.microsoft.com/dotnet/aspnet:10.0.12-resolute-chiseled@sha256:99a0fe6f3e933a3d170aa38d8be4237125b72c644a9229889ee842628cbaf845 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 9001

FROM mcr.microsoft.com/dotnet/sdk:10.0.401-resolute@sha256:e37606d2092c70f211d9e4f477dd7373b4b7d2b049399e4c6ff3a261243011c6 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY [".editorconfig", "."]
COPY ["Directory.Build.props", "."]
COPY ["src/UnfoldedCircle.OppoBluRay/UnfoldedCircle.OppoBluRay.csproj", "UnfoldedCircle.OppoBluRay/"]
COPY ["src/Oppo/Oppo.csproj", "Oppo/"]
RUN dotnet restore "UnfoldedCircle.OppoBluRay/UnfoldedCircle.OppoBluRay.csproj" --locked-mode
COPY ./src .

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
WORKDIR "/src/UnfoldedCircle.OppoBluRay"
RUN dotnet publish "UnfoldedCircle.OppoBluRay.csproj" -c "$BUILD_CONFIGURATION" -o /app/publish /p:UseAppHost=true
RUN touch /app/publish/configured_entities.json && \
    chown "$APP_UID" /app/publish/configured_entities.json && \
    chmod 644 /app/publish/configured_entities.json

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["./driver"]