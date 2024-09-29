FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 9001

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY [".editorconfig", "."]
COPY ["src/UnfoldedCircle.Server/UnfoldedCircle.Server.csproj", "UnfoldedCircle.Server/"]
RUN dotnet restore "UnfoldedCircle.Server/UnfoldedCircle.Server.csproj"
COPY ./src .
WORKDIR "/src/UnfoldedCircle.Server"
RUN dotnet build "UnfoldedCircle.Server.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "UnfoldedCircle.Server.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=true
RUN chown $APP_UID /app/publish/configured_entities.json && chmod 644 /app/publish/configured_entities.json

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["./driver"]