# Agent Instructions


## General information

### Project structure

| Path | Description |
|------|-------------|
| `DXMainClient/` | Main entry-point project — always the build target |
| `ClientCore/` | Core game-client logic |
| `ClientGUI/` | UI layer |
| `ClientUpdater/` | Auto-updater logic |
| `SecondStageUpdater/` | Secondary updater executable |
| `Rampastring.XNAUI/` | UI framework (git submodule) |
| `GitVersion.yml` | GitVersion branch and versioning strategy |
| `global.json` | Pins the required .NET SDK version (10.0, any feature band) |
| `Directory.Build.props` | MSBuild properties shared across all projects |
| `Directory.Packages.props` | Central NuGet package version management |
| `Docs/Build.md` | Human-oriented build documentation |

### Build the project

Always run restore before building. `SecondStageUpdater` is built via a custom MSBuild target (`BuildUpdater`) that fires for every DXMainClient build, but it is not in DXMainClient's project reference graph. This means the implicit restore triggered by `dotnet build` (without `--no-restore`) will not restore it, causing a build failure after any code change that invalidates the NuGet cache.

```shell
dotnet restore DXClient.slnx -p:Configuration=UniversalGLRelease
dotnet build DXMainClient/DXMainClient.csproj -p:Configuration=UniversalGLRelease -f net8.0 --no-restore
```

A successful build ends with `0 Error(s)`.

### Contributing guidelines
See [Contributing.md](../Contributing.md) for coding style, formatting, and other contribution guidelines. Be aware, Copilot, you MUST read and follow this file, even if the user did not explicitly ask you to.

## GitHub Copilot coding agent setup instructions

This section only applies to the GitHub Copilot coding agent, running in a Linux runner from the GitHub Action environment. It does not apply to other environments, such as local development.

The steps in the [copilot-coding-agent-setup.md](./copilot-coding-agent-setup.md) file are automatically executed via a GitHub Action workflow before the agent starts. **Only read and run them manually if you encounter a build failure**.