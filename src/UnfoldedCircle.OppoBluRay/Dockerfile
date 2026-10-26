FROM mcr.microsoft.com/dotnet/aspnet:11.0.0-rc.3-resolute-chiseled@sha256:1431d55bf5bd5708450c10664f3d4c4603d7e60cc059e64397787de3978f3e8c AS base
USER $APP_UID
WORKDIR /app
EXPOSE 9001

FROM mcr.microsoft.com/dotnet/sdk:11.0.100-rc.3-resolute@sha256:3a9175b6c54b2f076817af1360f691c9994447b192faa48a1a1f112a516844c7 AS build
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