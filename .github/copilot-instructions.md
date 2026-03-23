# Agent Instructions

## GitHub Copilot coding agent setup instructions

This section only applies to the GitHub Copilot coding agent, running in a Linux runner from the GitHub Action environment. It does not apply to other environments, such as local development.

The steps below are automatically executed before the agent starts via `.github/workflows/copilot-setup-steps.yml`. **Only run them manually if you encounter a build failure** — for example, if GitVersion cannot determine the version, if submodules are missing, or if NuGet restore fails.

### Step 1 — Initialize git submodules

`Rampastring.XNAUI` (and its nested submodule `Rampastring.Tools`) are **never** pre-initialized. Missing them causes compile errors about unknown `Rampastring.*` types.

```shell
git submodule update --init --recursive
```

### Step 2 — Unshallow the clone and fetch `develop`

The build system uses **GitVersion.MsBuild** to compute version numbers at compile time. It requires two things:

- A full (non-shallow) commit history.
- The `develop` branch reachable as a remote-tracking ref (it is the mainline branch in `GitVersion.yml`). Without it, any branch that is not `develop` or `master` fails with `Gitversion could not determine which branch to treat as the development branch`.

Run all three commands unconditionally:

- `--unshallow` is a no-op on an already-full clone (`|| true` prevents it from aborting).
- `set-branches` resets the remote's fetch refspec to the standard glob `+refs/heads/*:refs/remotes/origin/*`, removing any single-branch refspec that a shallow clone may have injected. Without this, LibGit2Sharp (used by GitVersion 5.12.0) crashes with `ref 'refs/remotes/origin/develop' doesn't match the destination` because it iterates refspecs in order and fails on the first non-matching one instead of falling through to the glob.
- The final fetch brings `refs/remotes/origin/develop` into the local ref store through that glob refspec so GitVersion can find it.

```shell
git fetch --unshallow origin || true
git remote set-branches origin '*'
git fetch origin develop
```

### Step 3 — Restore NuGet packages

Run restore from the **repo root** so that the solution file (`DXClient.slnx`) is used. This ensures all projects — including `SecondStageUpdater`, which the build pulls in transitively — are restored. Always pass the `Configuration` property; omitting it picks the wrong target frameworks.

```shell
dotnet restore -p:Configuration=UniversalGLRelease
```

### Step 4 — Build

```shell
dotnet build DXMainClient/DXMainClient.csproj -p:Configuration=UniversalGLRelease -f net8.0 --no-restore
```

A successful build ends with `0 Error(s)` and produces output under `bin/Release/UniversalGL/net8.0/`.

## General information

### Project structure

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

### Build the project

```shell
dotnet build DXMainClient/DXMainClient.csproj -p:Configuration=UniversalGLRelease -f net8.0 --no-restore
```

A successful build ends with `0 Error(s)` and produces output under `bin/Release/UniversalGL/net8.0/`.