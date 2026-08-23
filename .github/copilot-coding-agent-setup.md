# GitHub Copilot coding agent setup instructions

This section only applies to the GitHub Copilot coding agent, running in a Linux runner from the GitHub Action environment. It does not apply to other environments, such as local development.

The GitHub Actions workflow `.github/workflows/copilot-setup-steps.yml` runs the setup steps mentioned in this file automatically. The commands below are the manual equivalent and **should only be run if you encounter a build failure** — for example, if GitVersion cannot determine the version, if submodules are missing, or if NuGet restore fails.

## Step 1 — Initialize git submodules

`Rampastring.XNAUI` (and its nested submodule `Rampastring.Tools`) may not be pre-initialized. Missing them causes compile errors about unknown `Rampastring.*` types.

```shell
git submodule update --init --recursive
```

## Step 2 — Unshallow the clone and fetch `develop`

The build system uses **GitVersion.MsBuild** to compute version numbers at compile time. It requires two things:

- A full (non-shallow) commit history.
- The `develop` branch reachable as a remote-tracking ref (it is the mainline branch in `GitVersion.yml`). Without it, any branch that is not `develop` or `master` fails with `Gitversion could not determine which branch to treat as the development branch`.

Run all three commands unconditionally:

- `--unshallow` is a no-op on an already-full clone (`|| true` prevents it from aborting).
- `set-branches` resets the remote's fetch refspec to the standard glob `+refs/heads/*:refs/remotes/origin/*`, removing any single-branch refspec that a shallow clone may have injected. Without this, LibGit2Sharp (used by GitVersion 5.12.0) crashes with `ref 'refs/remotes/origin/develop' doesn't match the destination` because it iterates refspecs in order and fails on the first non-matching one instead of falling through to the glob.
- The final fetch brings `refs/remotes/origin/develop` into the local ref store through that glob refspec so GitVersion can find it.

The same fix must be applied to every submodule recursively: `Rampastring.XNAUI` and its nested `Rampastring.Tools` submodule also carry `GitVersion.MsBuild` and are subject to the same crash when checked out with a narrow single-branch refspec.

```shell
git fetch --unshallow origin || true
git remote set-branches origin '*'
git fetch origin develop
git submodule foreach --recursive \
  'git fetch --unshallow origin || true; git remote set-branches origin "*"; git fetch origin'
```

## Step 3 — Restore NuGet packages

Run restore from the **solution file** so that ALL projects — including `SecondStageUpdater` — are restored. `SecondStageUpdater` is not a `<ProjectReference>` of `DXMainClient`, but it is always built via the custom `BuildUpdater` MSBuild target. If it is not restored before the build, NETSDK1127 or NETSDK1004 errors occur. Always pass the `Configuration` property; omitting it picks the wrong target frameworks.

```shell
dotnet restore DXClient.slnx -p:Configuration=UniversalGLRelease
```

## Step 4 — Build

`--no-restore` is **required**. When `dotnet build` runs without it, the implicit restore only traverses DXMainClient's `<ProjectReference>` graph, which excludes `SecondStageUpdater`. This leaves SecondStageUpdater's restore assets stale after any code change, causing build failures. Always run Step 3 first, then build with `--no-restore`.

```shell
dotnet build DXMainClient/DXMainClient.csproj -p:Configuration=UniversalGLRelease -f net8.0 --no-restore
```

A successful build ends with `0 Error(s)`.
