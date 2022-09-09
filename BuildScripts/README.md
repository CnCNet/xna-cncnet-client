# Build & Publish #

The information below describes the steps that the default build script (Build-All.ps1) performs.

Game configurations
-------------------

The script will build the 3 supported Game configurations in Release mode:
* Ares
* TS
* YR

Engine configurations
---------------------

For each Game configuration several Engine configurations will be build, depending on the platform where the script is run:

On all platforms:
* UniversalGL

On Windows:
* UniversalGL
* WindowsDX
* WindowsGL
* WindowsXNA

TargetFramework configurations
------------------------------

For each Engine configuration one or more TargetFrameworks will be build:

UniversalGL:
* net6.0

WindowsDX, WindowsGL & WindowsXNA:
* net6.0-windows10.0.22000.0
* net48

Overview of the Engine configurations differences:

| | OS Support | Platform | Technology |
| - | ---------- | -------- | ---------- |
| UniversalGL | Any | AnyCPU | MonoGame DesktopGL |
| WindowsDX | Windows | AnyCPU | MonoGame WindowsDX + WinForms |
| WindowsGL | Windows | AnyCPU | MonoGame DesktopGL + WinForms |
| WindowsXNA | Windows | x86 | Microsoft XNA + WinForms |

AfterPublish PatchAppHost step
------------------------------

The file AfterPublish.targets will execute additional steps for the following build types:
* .NET6 WinForms Windows specific build
* .NET6 UniversalGL platform specific build (not part of Build-All)

Building a .NET 6 application results in an assembly, not in an executable, e.g. clientdx.dll.
On platform specific builds it also generates an apphost executable, so users have something to execute directly i.e. clientdx.exe.
All the .exe does is launch e.g.: "dotnet clientdx.dll".
The apphost creation is not configurable and always points to a dll with the same filename in the current directory.
Since we split them up into \clientdx.exe and \Resources\Binaries\Windows\clientdx.dll this breaks.
The AppHostPatcher modifies the .exe to point to the correct .dll path.

The AppHostPatcher application is located under \AdditionalFiles\AppHostPatcher.

Custom builds
-------------

It is possible to compile for a specfic platform in order to gain performance (PublishReadyToRun, PublishReadyToRunComposite) etc.

Manually compile linux x64 YR optimized binaries from command line:

>dotnet publish ..\DXMainClient\DXMainClient.csproj -c Release -f net6.0 -p:Engine=UniversalGL -p:Game=YR -o ..\Compiled\YR\net6.0\linux-x64 -r linux-x64 -p:PublishReadyToRun=true -p:PublishReadyToRunComposite=true

Or by updating the script BuildTools.ps1, which performs all needed operations such as structuring the files and folders:

>Build-Project -Configuration $Configuration -Game YR -Engine UniversalGL -TargetFramework net6.0 -RuntimeIdentifier linux-x64 -SkipMoveLibraries:$SkipMoveLibraries

>Build-Project -Configuration $Configuration -Game YR -Engine UniversalGL -TargetFramework net6.0 -RuntimeIdentifier win10-x64 -SkipMoveLibraries:$SkipMoveLibraries

>Build-Project -Configuration $Configuration -Game YR -Engine UniversalGL -TargetFramework net6.0 -RuntimeIdentifier osx.12-x64 -SkipMoveLibraries:$SkipMoveLibraries

>Build-Project -Configuration $Configuration -Game YR -Engine UniversalGL -TargetFramework net6.0 -RuntimeIdentifier ubuntu.22.04-x64 -SkipMoveLibraries:$SkipMoveLibraries

>Build-Project -Configuration $Configuration -Game YR -Engine UniversalGL -TargetFramework net6.0 -RuntimeIdentifier alpine.3.9-arm -SkipMoveLibraries:$SkipMoveLibraries

Build output
------------

The build output per Game will look like below.

![unknown](https://user-images.githubusercontent.com/25006126/189449430-07bfb4b5-bc5f-4cea-870e-90d1870b8fe8.png)

The cross-platform UniversalGL build (net6.0\any) will not contain an executable, only a clientogl.dll in \net6.0\any\Resources\Binaries\OpenGL.
Which is to be executed with:

`dotnet clientogl.dll`
