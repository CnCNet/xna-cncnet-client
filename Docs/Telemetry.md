# Anonymous usage statistics (telemetry)

About telemetry
-----------------------------------
The client can send anonymous usage statistics to [TelemetryDeck](https://telemetrydeck.com/). This tells you which operating systems, hardware, renderers, resolutions and client versions your players use, and how many players are active.

Telemetry is off unless your mod/game sets a TelemetryDeck app ID. When it is set, telemetry is on by default and players can turn it off in the options.

How to set up telemetry for your mod/game
-----------------------------------
1. Create an account and an app on [TelemetryDeck](https://dashboard.telemetrydeck.com/).
2. Copy the app ID and insert it as a value of `Resources/ClientDefinitions.ini`->`[Settings]`->`TelemetryDeckAppID` key.
3. If your TelemetryDeck organization has a namespace, insert it as a value of the `TelemetryDeckNamespace` key. TelemetryDeck recommends sending signals to the namespace endpoint when there is one.

```ini
[Settings]
TelemetryDeckAppID=00000000-0000-0000-0000-000000000000
TelemetryDeckNamespace=com.example
```

Debug builds of the client mark their signals as test signals, so they don't show up in your production data.

What players see
-----------------------------------
- The privacy notice at the bottom of the main menu has an extra paragraph about telemetry. It is shown until the player clicks `Got it`.
- The `CnCNet` tab of the options window has a `Send anonymous usage statistics` checkbox (`chkTelemetry`).

The texts can be translated with the `Client:Main:TelemetryText`, `Client:DTAConfig:SendTelemetry` and `Client:DTAConfig:SendTelemetryTooltip` keys.

Privacy
-----------------------------------
The client never sends an identifier for the player or the machine. Every client launch reports under a new random ID that is not stored anywhere. Nicknames, chat, IP addresses and file paths are never sent. Values that could tell machines apart are rounded: memory is grouped into common sizes, and uncommon resolutions are reported as `Other`.

The settings INI only stores the date of the last launch and the month of installation, in the `[Telemetry]` section.

Signals
-----------------------------------
Every signal has these values:

| Key | Example |
|---|---|
| `Client.version` | `2.12.5.0` |
| `Client.engine` | `WindowsDX`, `WindowsGL`, `WindowsXNA` or `UniversalGL` |
| `Client.runtime` | `.NET 8.0.10` |
| `Game.name` | the `LocalGame` value |
| `Game.version` | the game version from the updater |

### `Client.started`

Sent once per client launch.

| Key | Description |
|---|---|
| `Activity.firstLaunchToday` | Whether this is the first launch on this installation today (UTC). |
| `Activity.firstLaunchThisWeek` | Whether this is the first launch this week (UTC, weeks start on Monday). |
| `Activity.firstLaunchThisMonth` | Whether this is the first launch this month (UTC). |
| `Activity.newInstall` | Whether this is the first launch of a new installation. |
| `Activity.installMonth` | The month the installation was first started, like `2026-10`. `Unknown` for installations from before telemetry was added. |
| `System.os` | One of `Win7`, `Win8`, `Win8.1`, `Win10`, `Win11`, `Wine`, `Linux`, `macOS` or `Other`. Wine is detected first, because Wine can report any Windows version. |
| `System.osBuild` | The Windows build number. Not sent under Wine or on other operating systems. |
| `System.win7SP1` | Whether Windows 7 has Service Pack 1. Only sent on Windows 7. |
| `System.osDescription`, `System.osArchitecture`, `System.processArchitecture` | The full operating system description and the architectures. |
| `System.cpu`, `System.logicalProcessors` | Processor model and number of logical processors. |
| `System.gpu` | Active graphics adapters. |
| `System.memory` | Installed memory, rounded to `4 GB or less`, `8 GB`, `16 GB`, `32 GB`, `64 GB` or `128 GB or more`. |
| `System.desktopResolution` | Desktop resolution. |
| `System.culture` | The system's regional format, like `en-US`. |
| `Settings.*` | Client settings: translation, theme, renderer, in-game and client resolution, windowed and borderless modes, update checks, Discord and Steam integration, P2P. |

Processor, graphics adapter and memory information is only sent on Windows.

Counting players
-----------------------------------
Because there is no identifier, TelemetryDeck's built-in user counts show client launches, not players. To count players, count `Client.started` signals with a filter:

- Daily active players: `Activity.firstLaunchToday` is `true`.
- Weekly active players: `Activity.firstLaunchThisWeek` is `true`.
- Monthly active players: `Activity.firstLaunchThisMonth` is `true`.
- New players: `Activity.newInstall` is `true`.

Use the same filters to see hardware and settings per player instead of per launch. For example, to see which graphics adapters your monthly players use, break down `System.gpu` for signals where `Activity.firstLaunchThisMonth` is `true`.

For example, the Windows 7 share of your players is the share of `System.os` = `Win7` among `Client.started` signals where `Activity.firstLaunchThisMonth` is `true`.

To estimate how many players keep playing, compare the new installations of a month with the monthly active players that have that `Activity.installMonth` in later months.

A player who reinstalls the game or deletes the settings INI is counted again as a new installation.
