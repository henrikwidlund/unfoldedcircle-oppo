# 10.0.8
FROM mcr.microsoft.com/dotnet/aspnet@sha256:a7f704b4b87284c80df9b90ab0e06810a241785204dc191ec05f2b26b5847623 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 9001

# 10.0.300
FROM mcr.microsoft.com/dotnet/sdk@sha256:69ff714a42f7931475acfcd2792d69cd4a656e4f3653d520e25c0fbe3c6d0cba AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY [".editorconfig", "."]
COPY ["src/UnfoldedCircle.OppoBluRay/UnfoldedCircle.OppoBluRay.csproj", "UnfoldedCircle.OppoBluRay/"]
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