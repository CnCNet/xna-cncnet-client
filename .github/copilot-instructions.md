# GitHub Copilot Instructions

You already have this repository checked out. Run the commands below in order every time you start a new session before making or building any changes.

## Step 1 — Initialize git submodules

`Rampastring.XNAUI` (and its nested submodule `Rampastring.Tools`) are **never** pre-initialized. Missing them causes compile errors about unknown `Rampastring.*` types.

```shell
git submodule update --init --recursive
```

## Step 2 — Unshallow the clone and fetch `develop`

The build system uses **GitVersion.MsBuild** to compute version numbers at compile time. It requires two things:

- A full (non-shallow) commit history.
- The `develop` branch reachable as a remote-tracking ref (it is the mainline branch in `GitVersion.yml`). Without it, any branch that is not `develop` or `master` fails with `Gitversion could not determine which branch to treat as the development branch`.

Run both commands unconditionally. The first is a no-op when already unshallowed (the `|| true` prevents it from aborting); the second is a no-op when `origin/develop` is already up to date.

```shell
git fetch --unshallow origin || true
git fetch origin develop:refs/remotes/origin/develop
```

## Step 3 — Restore NuGet packages

Run restore from the **repo root** so that the solution file (`DXClient.slnx`) is used. This ensures all projects — including `SecondStageUpdater`, which the build pulls in transitively — are restored. Always pass the `Configuration` property; omitting it picks the wrong target frameworks.

```shell
dotnet restore -p:Configuration=UniversalGLRelease
```

## Step 4 — Build

```shell
dotnet build DXMainClient/DXMainClient.csproj -p:Configuration=UniversalGLRelease -f net8.0 --no-restore
```

A successful build ends with `0 Error(s)` and produces output under `bin/Release/UniversalGL/net8.0/`.

## Publish (optional)

```shell
dotnet publish DXMainClient/DXMainClient.csproj --configuration UniversalGLRelease --framework net8.0 --output Compiled/Resources/BinariesNET8/UniversalGL
```

## Project structure

| Path | Description |
|------|-------------|
| `DXMainClient/` | Main entry-point project — always the build target |
| `ClientCore/` | Core game-client logic |
| `ClientGUI/` | UI layer |
| `ClientUpdater/` | Auto-updater logic |
| `SecondStageUpdater/` | Secondary updater executable |
| `Rampastring.XNAUI/` | UI framework (git submodule — must be initialized, see Step 1) |
| `GitVersion.yml` | GitVersion branch and versioning strategy |
| `global.json` | Pins the required .NET SDK version (10.0, any feature band) |
| `Directory.Build.props` | MSBuild properties shared across all projects |
| `Directory.Packages.props` | Central NuGet package version management |
| `Docs/Build.md` | Human-oriented build documentation |
