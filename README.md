# Unfolded Circle Oppo/Magnetar Integration Driver

[![Release](https://img.shields.io/github/actions/workflow/status/henrikwidlund/unfoldedcircle-oppo/github-release.yml?label=Release&logo=github)](https://github.com/henrikwidlund/unfoldedcircle-oppo/actions/workflows/github-release.yml)
[![CI](https://img.shields.io/github/actions/workflow/status/henrikwidlund/unfoldedcircle-oppo/ci.yml?label=CI&logo=github)](https://github.com/henrikwidlund/unfoldedcircle-oppo/actions/workflows/ci.yml)
![Sonar Quality Gate](https://img.shields.io/sonar/quality_gate/henrikwidlund_unfoldedcircle-oppo?server=https%3A%2F%2Fsonarcloud.io&label=Sonar%20Quality%20Gate&logo=sonarqube)
[![Qodana](https://img.shields.io/github/actions/workflow/status/henrikwidlund/unfoldedcircle-oppo/qodana_code_quality.yml?branch=main&label=Qodana&logo=github)](https://github.com/henrikwidlund/unfoldedcircle-oppo/actions/workflows/qodana_code_quality.yml)
[![Docker](https://img.shields.io/github/actions/workflow/status/henrikwidlund/unfoldedcircle-oppo/docker.yml?label=Docker&logo=docker)](https://github.com/henrikwidlund/unfoldedcircle-oppo/actions/workflows/docker.yml)

This repository contains the server code for hosting an Oppo/Magnetar Blu-ray integration driver for the Unfolded Circle Remotes, Linux, macOS, Windows and Docker Images.

## Supported devices

- Oppo BDP-83
- Oppo BDP-93
- Oppo BDP-95
- Oppo BDP-103
- Oppo BDP-105
- Oppo UDP-203
- Oppo UDP-205
- Magnetar Players

### Supported features limitations

- All features are supported for the UDP-20X series.
- The Oppo and Magnetar players only allows one connection at a time. If you have multiple remotes or other systems that you want to connect to the same player, then you'll need to use the [Oppo Multiplexer](https://github.com/henrikwidlund/oppo-multiplexer) in order to avoid getting continuous disconnects and failed commands.
- Track Name/Album Name/Album Cover/Artist Name is only available while playing audio (not movies). It's attempted on every BDP-83/93/95/10X player, but firmware support for the underlying API is only confirmed on BDP-103/105 (❓ below) - an unsupported player/content combination simply won't show this data rather than erroring.

| Feature            | Oppo BDP-83/93/95 | Oppo BDP-10X | Magnetar |
|--------------------|-------------------|--------------|----------|
| Option Command     | ❌                 | ✔️           | ✔️       |
| 3D Switching       | ❌                 | ✔️           | ❌️️      |
| Picture Adjustment | ❌                 | ✔️           | ❌️       |
| HDR Mode           | ❌                 | ❌️           | ✔️       |
| Info Hold          | ❌                 | ❌️           | ❌️       |
| Resolution Hold    | ❌                 | ❌️           | ❌️       |
| A/V Sync           | ❌                 | ❌️           | ❌️       |
| Gapless Playback   | ❌                 | ❌️           | ❌️       |
| Track Name         | ❓                 | ✔️           | ✔️       |
| Album Name         | ❓                 | ✔️           | ✔️       |
| Album Cover        | ❓                 | ✔️           | ✔️       |
| Artist Name        | ❓                 | ✔️           | ✔️       |

## Prerequisites

### Running

- The published binaries are self-contained and doesn't require any additional software.
- Use the [Docker Image](https://hub.docker.com/r/henrikwidlund/unfoldedcircle-oppo).

### Network

| Service      | Port  | Protocol   |
|--------------|-------|------------|
| Server       | 9001* | HTTP (TCP) |
| Oppo BDP-83  | 19999 | TCP        |
| Oppo BDP-9X  | 48360 | TCP        |
| Oppo BDP-10X | 48360 | TCP        |
| Oppo UDP-20X | 23    | TCP        |

\* Server port can be adjusted by specifying the desired port with the `UC_INTEGRATION_HTTP_PORT` environment variable. Port can't be controlled when running on the remote.

A player connected over RS-232 instead is reached on whatever port its serial bridge listens on - see [Connecting over RS-232](#connecting-over-rs-232).

### Development

- [dotnet 11 SDK](https://dotnet.microsoft.com/download/dotnet/11.0).
- or [Docker](https://www.docker.com/get-started).

## Installing on the remote

1. Download `unfolded-circle-oppo-[version]-remote.tar.gz` from the release page
2. Open the remote's Web Configurator
3. Click on `Integrations`
4. Click on `Add new` and then `Install custom`
5. Choose the file in step 1 (`unfolded-circle-oppo-[version]-remote.tar.gz`)
6. Make sure that your device is turned on
7. Click on the newly installed integration and follow the on-screen instructions

## Connecting over RS-232

Every supported Oppo also accepts the same commands on its RS-232 port, so a player can be controlled
without any network connection at all. The driver speaks TCP, so the serial port has to be bridged to TCP
by something on the network.

This has been tested with **an Unfolded Circle Dock 3**, with a port set to `RS232` (port 1 is TCP 4999,
port 2 is TCP 5000). The driver asks nothing of the bridge beyond a TCP port that passes bytes through
unchanged, so other serial device servers - a Global Cache IP2SL or iTach, a USR-TCP232, `ser2net` on a
Linux box with a USB-serial adapter - should work the same way. None of those has been tried, so if you
use one, the wiring and port numbers below are a starting point rather than a recipe.

Set the bridge to **9600 baud, 8 data bits, no parity, 1 stop bit, no flow control**. These are fixed on
the player and cannot be changed.

Wiring depends on the bridge. The player's connector is a **female DB9 wired as a DCE**, and only three
pins are used:

| Player pin | Signal                |
|-----------:|-----------------------|
| 2          | TXD - player sends    |
| 3          | RXD - player receives |
| 5          | GND                   |

- **A bridge with its own DB9** (iTach, IP2SL, USR-TCP232, a PC serial port) takes a plain
  straight-through DB9 cable. Do *not* use a null modem cable - the player is already a DCE.
- **A Dock 3** has 3.5 mm TRS jacks rather than a DB9, so it needs a TRS-to-DB9 cable. [The dock's RS232
  pinout](https://support.unfoldedcircle.com/hc/en-us/articles/20028572717084-Dock-3-3-5-mm-jack-pinout)
  is tip = TX, ring = RX, sleeve = GND, so the cable crosses:

  | 3.5 mm jack (dock) | Male DB9 (to the player) |
  |--------------------|--------------------------|
  | Tip - dock TX      | pin 3 - player RxD       |
  | Ring - dock RX     | pin 2 - player TxD       |
  | Sleeve - GND       | pin 5 - GND              |

  The StarTech `9M351M-RS232-CABLE` (male DB9 to 3.5 mm, 1 m) is wired this way off the shelf and mates
  directly with the player's female connector. Generic "3.5 mm to DB9" cables are not wired to any one
  standard and some use the opposite convention, so check the datasheet or buzz it out with a multimeter.
  Getting it backwards breaks nothing - the player simply never answers, and swapping tip and ring fixes it.

Two Dock 3 notes, both from [the Denon RS-232 integration](https://github.com/run51/uc-integration-denon-rs232),
which drives a DCE RS-232 device from a dock over exactly this cable:

- Do not hot-plug. Connect the cable before powering the dock, or it may brown out.
- If every command comes back to you unchanged, the plug is not seated all the way in and the dock is
  hearing its own transmit.

A Dock 3 port must also be set to `RS232` mode, and its TCP server enabled separately - that is a distinct
checkbox, off by default, and until it is on nothing listens on 4999 or 5000. Both are set in the dock's own
web interface rather than on the Remote, which as of dock firmware 0.10.5 does not offer the RS232 mode.

**Use port 2 (TCP 5000).** Port 1 carries the dock's own boot messages on its transmit line, which the
player reads as commands.

To configure it, start setup as usual and choose **Connect over RS-232 through a serial bridge** instead of
a discovered player, then enter the bridge's address and port.

Give the bridge a **fixed address** first - a DHCP reservation on your router, or a static IP. The address
is part of the player's identity here, and reconfiguring deliberately does not let you change it, so a
bridge that moves to a new address means removing the player and adding it again. Everything after that behaves the same:
the RS-232 and IP command sets are identical.

What differs:

- Discovery does not find a serial-only player - it has nothing to answer on.
- Wake-on-LAN and the Magnetar protocol need a network path to the player, so Magnetar is not offered here.
- Track/album/artist on BDP-83/93/95/10X comes from the player's own HTTP API, which a serial-only player
  does not expose. The 20X series reads this over the control connection and is unaffected.
- Most bridges accept several TCP clients at once and forward bytes from all of them onto one UART, which
  interleaves commands. A Dock 3 takes up to 8 clients per port and broadcasts everything the player says
  to all of them. Point only one controller at a given serial port.

A connected bridge does not by itself prove the player is there - the TCP connection terminates at the
bridge, which stays up whether or not the player behind it does. Nothing extra is done about that: every
poll queries power status anyway, so a player that has stopped answering shows up as unknown on the next
poll, and commands report their own failure.

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

- Selecting input on the player can only be done when the player reports that it is on, this means that you have to place a delay between the `Switch on` and `Input source` commands if you want to use this in the `On sequence`, or the remote will think the start sequence fails. This is because the player may accept power commands and report success while still transitioning between states — for example, sending Power On shortly after Power Off may return success but the player continues shutting down. When using macros or sequences, place a delay of a couple of seconds after any power state change before sending subsequent commands.
- The artist, album and track information might not always be available or accurate - the integration displays whatever information the player provides.
- The album cover might be incorrect or missing. This is because the CDDB database, which the Oppo players use, no longer exists, as such, the application tries to get covers by matching the current artist and album. This is not always accurate enough.
- Reported progress follows the unit's own front-panel time display. Movie progress only supports total (whole-title) length, the player's default. Changing the display to something else (e.g. chapter time) for movies makes progress inconsistent or missing. Leave the player's time display on its defaults for accurate progress.

## Licenses / Copyright

- [License](LICENSE)
- [richardschneider/net-dns](https://github.com/richardschneider/net-dns/blob/master/LICENSE)
- [richardschneider/net-mdns](https://github.com/richardschneider/net-mdns/blob/master/LICENSE)
- [jdomnitz/net-dns](https://github.com/jdomnitz/net-dns/blob/master/LICENSE)
- [jdomnitz/net-mdns](https://github.com/jdomnitz/net-mdns/blob/master/LICENSE)
