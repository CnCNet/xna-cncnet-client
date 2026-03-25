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
| `Rampastring.XNAUI/` | UI framework (git submodule — must be initialized) |
| `GitVersion.yml` | GitVersion branch and versioning strategy |
| `global.json` | Pins the required .NET SDK version (10.0, any feature band) |
| `Directory.Build.props` | MSBuild properties shared across all projects |
| `Directory.Packages.props` | Central NuGet package version management |
| `Docs/Build.md` | Human-oriented build documentation |

### Build the project

```shell
dotnet build DXMainClient/DXMainClient.csproj -p:Configuration=UniversalGLRelease -f net8.0
```

A successful build ends with `0 Error(s)`.

### Contributing guidelines
See [Contributing.md](../Contributing.md) for coding style, formatting, and other contribution guidelines.

## GitHub Copilot coding agent setup instructions

This section only applies to the GitHub Copilot coding agent, running in a Linux runner from the GitHub Action environment. It does not apply to other environments, such as local development.

The steps in [copilot-coding-agent-setup.md](./copilot-coding-agent-setup.md) file are automatically executed before the agent starts. **Only read and run them manually if you encounter a build failure**.