FROM mcr.microsoft.com/dotnet/aspnet:11.0-resolute-chiseledd@sha256:fc0ee12f9ea79bde6cda88c7a34e5467fe383dbb62276be0ec13e5e9cc1654ac AS base
USER $APP_UID
WORKDIR /app
EXPOSE 9001

FROM mcr.microsoft.com/dotnet/sdk:11.0.100-resolute@sha256:65f3c7c6a25a239ab3e6719f2896147e7b68242ff2d8818dd70498bde6ef8bea AS build
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