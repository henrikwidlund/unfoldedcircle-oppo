FROM mcr.microsoft.com/dotnet/aspnet:11.0.0-rc.3-resolute-chiseled@sha256:7bb2935a9c058ececbeadcdc26c7ccc20f2a193c8efa7764123ef7b83cd4e179 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 9001

FROM mcr.microsoft.com/dotnet/sdk:11.0.100-rc.3-resolute@sha256:59d092a889fbad09c1a0b0c5dd072c6315125b0294a5cddfa48ec892f63b2ab3 AS build
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