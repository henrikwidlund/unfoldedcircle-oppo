# Unfolded Circle Oppo Integration Driver

[![Release](https://img.shields.io/github/actions/workflow/status/henrikwidlund/unfoldedcircle-oppo/github-release.yml?label=Release&logo=github)](https://github.com/henrikwidlund/unfoldedcircle-oppo/actions/workflows/github-release.yml)
[![CI](https://img.shields.io/github/actions/workflow/status/henrikwidlund/unfoldedcircle-oppo/ci.yml?label=CI&logo=github)](https://github.com/henrikwidlund/unfoldedcircle-oppo/actions/workflows/ci.yml)
![Sonar Quality Gate](https://img.shields.io/sonar/quality_gate/henrikwidlund_unfoldedcircle-oppo?server=https%3A%2F%2Fsonarcloud.io&label=Sonar%20Quality%20Gate&logo=sonarqube)
[![Qodana](https://img.shields.io/github/actions/workflow/status/henrikwidlund/unfoldedcircle-oppo/qodana_code_quality.yml?branch=main&label=Qodana&logo=github)](https://github.com/henrikwidlund/unfoldedcircle-oppo/actions/workflows/qodana_code_quality.yml)
[![Docker](https://img.shields.io/github/actions/workflow/status/henrikwidlund/unfoldedcircle-oppo/docker.yml?label=Docker&logo=docker)](https://github.com/henrikwidlund/unfoldedcircle-oppo/actions/workflows/docker.yml)

This repository contains the server code for hosting an Oppo Blu-ray integration driver for the Unfolded Circle Remotes.

## Supported devices

- Oppo BDP-83
- Oppo BDP-93
- Oppo BDP-95
- Oppo BDP-103
- Oppo BDP-105
- Oppo UDP-203
- Oppo UDP-205

### Supported features limitations

- All features are supported for the UDP-20X series.
- The Oppo players only allows one connection at a time, so if you have multiple remotes or other systems connected to the same player,
the will keep getting disconnected and commands will fail.

| Feature            | Oppo BDP-83/93/95 | Oppo BDP-10X |
|--------------------|-------------------|--------------|
| Option Command     | ❌                 | ✔️           |
| 3D Switching       | ❌                 | ✔️           |
| Picture Adjustment | ❌                 | ✔️           |
| HDR Mode           | ❌                 | ❌️           |
| Info Hold          | ❌                 | ❌️           |
| Resolution Hold    | ❌                 | ❌️           |
| A/V Sync           | ❌                 | ❌️           |
| Gapless Playback   | ❌                 | ❌️           |
| Track Name         | ❌                 | ❌️           |
| Album Name         | ❌                 | ❌️           |
| Album Cover        | ❌                 | ❌️           |
| Artist Name        | ❌                 | ❌️           |

## Prerequisites

### Running

- The published binary is self-contained and doesn't require any additional software. It's compiled for Linux ARM64 and is meant to be running on the remote.
- Use the [Docker Image](https://hub.docker.com/r/henrikwidlund/unfoldedcircle-oppo) in the [Core Simulator](https://github.com/unfoldedcircle/core-simulator)

### Network

| Service      | Port  | Protocol   |
|--------------|-------|------------|
| Server       | 9001* | HTTP (TCP) |
| Oppo BDP-83  | 19999 | TCP        |
| Oppo BDP-9X  | 48360 | TCP        |
| Oppo BDP-10X | 48360 | TCP        |
| Oppo UDP-20X | 23    | TCP        |

\* Server port can be adjusted by specifying the desired port with the `UC_INTEGRATION_HTTP_PORT` environment variable.

### Development

- [dotnet 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0).
- or [Docker](https://www.docker.com/get-started).

## Installing on the remote

1. Download `unfolded-circle-oppo-[version]-remote.tar.gz` from the release page
2. Open the remote's Web Configurator
3. Click on `Integrations`
4. Click on `Add new` and then `Install custom`
5. Choose the file in step 1 (`unfolded-circle-oppo-[version]-remote.tar.gz`)
6. Make sure that your device is turned on
7. Click on the newly installed integration and follow the on-screen instructions

## Configuration

The application can be configured using the `appsettings.json` file or environment variables.
Additionally, the application saves configured entities to the `configured_entities.json` file, which will be saved to the directory specified by the `UC_CONFIG_HOME` environment variable.

## Logging

By default, the application logs to stdout. 
You can customize the log levels by either modifying the `appsettings.json` file or by setting environment variables.

### Log levels
- `Trace`
- `Debug`
- `Information`
- `Warning`
- `Error`

`Trace` log level will log the contents of all the incoming and outgoing requests and responses. This includes both Websockets and Telnet. 

### `appsettings.json`

```json
{
    "Logging": {
        "LogLevel": {
          "UnfoldedCircle.Server": "Information",
          "UnfoldedCircle.OppoBluRay": "Information",
          "Oppo": "Information",
          "Makaretu.Dns": "Warning"
        }
    }
}
```

### Environment variables

Same adjustments to log levels can be made by setting environment variables.
- `Logging__LogLevel__UnfoldedCircle.Server` = `Information`
- `Logging__LogLevel__UnfoldedCircle.OppoBluRay` = `Information`
- `Logging__LogLevel__Oppo` = `Information`
- `Logging__LogLevel__Makaretu.Dns` = `Warning`

## Building from source code

### Building for the remote

Execute `publish.sh` script to build the application for the remote. This will produce a `tar.gz` file in the root of the repository.

### Building for Docker

Execute the following from the root of the repository:

```sh
docker build -f src/UnfoldedCircle.OppoBluRay/Dockerfile -t oppo .
```

### dotnet CLI

```sh
dotnet publish ./src/UnfoldedCircle.OppoBluRay/UnfoldedCircle.BluRayPlayer.csproj -c Release --self-contained -o ./publish
```

This will produce a self-contained binary in the `publish` directory in the root of the repository.

## Limitations

- Selecting input on the player can only be done when the player reports that it is on, this means that you have to place a delay between the `Switch on` and `Input source` commands if you want to use this in the `On sequence`, or the remote will think the start sequence fails.
- The artist, album and track information might not always be available or accurate. This can't be helped as it's the information the player provides.
- The album cover might be incorrect or missing. This is because the CDDB database no longer exists, as such, the application tries to get covers by matching the current artist and album. This is not always accurate enough.

## Licenses / Copyright

- [License](LICENSE)
- [richardschneider/net-dns](https://github.com/richardschneider/net-dns/blob/master/LICENSE)
- [richardschneider/net-mdns](https://github.com/richardschneider/net-mdns/blob/master/LICENSE)
- [jdomnitz/net-dns](https://github.com/jdomnitz/net-dns/blob/master/LICENSE)
- [jdomnitz/net-mdns](https://github.com/jdomnitz/net-mdns/blob/master/LICENSE)
