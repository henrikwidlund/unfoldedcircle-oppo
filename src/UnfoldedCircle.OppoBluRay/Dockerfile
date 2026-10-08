FROM mcr.microsoft.com/dotnet/aspnet:11.0.0-rc.1-resolute-chiseled@sha256:af930df2f1e6da76666d818c5922534cc7f2a4077adfae27e7c81c28a6406d29 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 9001

FROM mcr.microsoft.com/dotnet/sdk:11.0.100-rc.1-resolute@sha256:d0a39ca4acefe992f26030f8fb8c60e1b14ac088822cb3fdfbe536ecf843a675 AS build
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