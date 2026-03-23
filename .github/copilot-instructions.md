# GitHub Copilot Instructions

This document provides guidance for AI coding agents (such as GitHub Copilot) working on this repository. It covers the two most common failure points when building the project in an automated or agent environment on Linux.

## Building on Linux (UniversalGL)

The only configuration supported on Linux is **UniversalGL** (`net8.0`). Windows-only configurations (`WindowsDX`, `WindowsGL`, `WindowsXNA`) require Windows and are not available on Linux.

### Prerequisites

- [.NET SDK 10.0](https://dotnet.microsoft.com/en-us/download/dotnet/10.0) — required by `global.json` (the `rollForward` policy will accept any 10.x feature-band release)
- Git with submodule support
- PowerShell 7.2 or newer (optional, only required for the `Scripts/build.ps1` helper script)

### Step 1 — Clone with Git (never download a ZIP archive)

The repository **must** be cloned with Git, not downloaded as a ZIP. The project relies on Git metadata for versioning (GitVersion) and for submodule tracking.

```shell
git clone https://github.com/SadPencil/xna-cncnet-client.git
cd xna-cncnet-client
```

### Step 2 — Initialize git submodules

The `Rampastring.XNAUI` library (and its own submodule `Rampastring.Tools`) are tracked as git submodules. They are **not** included in the repository checkout automatically and must be initialized explicitly:

```shell
git submodule update --init --recursive
```

Skipping this step will cause compilation errors because source files inside `Rampastring.XNAUI/` will be missing.

### Step 3 — Ensure a full git history and required branches for GitVersion

This project uses [GitVersion.MsBuild](https://gitversion.net/) to automatically compute the assembly version at build time. GitVersion requires:

1. **A full (non-shallow) commit history.** Shallow clones (e.g., created with `git clone --depth 1` or by many CI systems by default) will cause GitVersion to fail. Unshallow the clone if needed:

   ```shell
   git fetch --unshallow origin
   ```

2. **The `develop` branch must be reachable** (as a local branch or a remote-tracking ref). GitVersion's configuration (`GitVersion.yml`) defines `develop` and `master` as the mainline branches. When working on any other branch (such as a feature or Copilot-generated branch), GitVersion needs to see at least one of those branches to inherit its version-calculation strategy. Fetch it explicitly if it is not already present:

   ```shell
   git fetch origin develop:refs/remotes/origin/develop
   ```

   If neither `develop` nor `master` is reachable, the build will fail with:
   > `Gitversion could not determine which branch to treat as the development branch`

### Step 4 — Restore NuGet packages

```shell
dotnet restore DXMainClient/DXMainClient.csproj -p:Configuration=UniversalGLRelease
```

### Step 5 — Build

```shell
dotnet build DXMainClient/DXMainClient.csproj \
    -p:Configuration=UniversalGLRelease \
    -f net8.0 \
    --no-restore
```

A successful build produces output under `bin/Release/UniversalGL/net8.0/`.

### Step 6 — Publish (optional, creates a deployable package)

```shell
dotnet publish DXMainClient/DXMainClient.csproj \
    --configuration UniversalGLRelease \
    --framework net8.0 \
    --output Compiled/Resources/BinariesNET8/UniversalGL
```

Alternatively, use the included PowerShell build script (requires PowerShell 7.2+), which handles all configurations and output folder layout automatically:

```shell
pwsh Scripts/build.ps1
```

On Linux, `build.ps1` will only build the `UniversalGL` configuration because the Windows-only configurations are skipped when `$IsWindows` is false.

## Summary of common failure causes

| Symptom | Root cause | Fix |
|---------|-----------|-----|
| Missing source files / compile errors about `Rampastring.*` types | Git submodules not initialized | `git submodule update --init --recursive` |
| `Gitversion could not determine which branch to treat as the development branch` | `develop` (or `master`) branch not reachable locally | `git fetch origin develop:refs/remotes/origin/develop` |
| GitVersion exits with code 1 and mentions "shallow" | Shallow clone with no full history | `git fetch --unshallow origin` |
| `NETSDK1004: Assets file … not found` | NuGet restore not run, or run without the correct `Configuration` property | `dotnet restore … -p:Configuration=UniversalGLRelease` |

## Project structure quick reference

| Path | Description |
|------|-------------|
| `DXMainClient/` | Main entry-point project; the build target for all configurations |
| `ClientCore/` | Core game-client logic |
| `ClientGUI/` | UI layer |
| `ClientUpdater/` | Auto-updater logic |
| `SecondStageUpdater/` | Secondary updater executable |
| `Rampastring.XNAUI/` | UI framework (git submodule) |
| `Scripts/build.ps1` | PowerShell build script |
| `Docs/Build.md` | Human-oriented build documentation |
| `GitVersion.yml` | GitVersion branch and versioning strategy |
| `global.json` | Pins the required .NET SDK version |
| `Directory.Build.props` | MSBuild properties shared across all projects |
| `Directory.Packages.props` | Central NuGet package version management |
