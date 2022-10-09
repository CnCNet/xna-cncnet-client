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
* net6.0-windows
* net48

Overview of the Engine configurations differences:

| | OS Support | Default Platform | Technology |
| - | ---------- | -------- | ---------- |
| UniversalGL | Any | AnyCPU (64-bit preferred) | MonoGame DesktopGL |
| WindowsDX | Windows | AnyCPU (64-bit preferred) | MonoGame WindowsDX + WinForms |
| WindowsGL | Windows | AnyCPU (64-bit preferred) | MonoGame DesktopGL + WinForms |
| WindowsXNA | Windows | AnyCPU (32-bit preferred) | Microsoft XNA + WinForms |

AfterPublish PatchAppHost step
------------------------------

The file AfterPublish.targets will execute additional steps for the following build types:
* .NET6 WinForms Windows specific build
* .NET6 UniversalGL platform specific build (not part of Build-All)

Building a .NET 6 application results in an assembly, not in an executable, e.g. `clientdx.dll`. On platform specific builds it also generates an apphost executable, so users have something to execute directly i.e. `clientdx.exe`. All the .exe does is launch e.g.: `dotnet clientdx.dll`.

By default the apphost always points to a dll with the same filename in the current directory. Since we split them up into `\clientdx.exe` and `\Resources\Binaries\Windows\clientdx.dll` this breaks. `CreateAppHost.targets` modifies the .exe to point to the correct .dll path.

Custom builds
-------------

It is possible to compile for a specfic platform in order to gain performance (`PublishReadyToRun`, `PublishReadyToRunComposite`) etc.

Manually compile linux x64 YR optimized binaries from command line:

>dotnet publish ..\DXMainClient\DXMainClient.csproj -c Release -p:Game=YR -p:Engine=UniversalGL -f net6.0 -o ..\Compiled\YR\net6.0\linux-x64\Resources\Binaries\OpenGL -r linux-x64 -p:PublishReadyToRun=true -p:PublishReadyToRunComposite=true

Build output
------------

The build output when using the `dotnet publish` command is created in `\Compiled` and will look like below for each Game.

![unknown](https://user-images.githubusercontent.com/25006126/189449430-07bfb4b5-bc5f-4cea-870e-90d1870b8fe8.png)

The cross-platform UniversalGL build (net6.0\any) will not contain an executable, only a `clientogl.dll` in `\net6.0\any\Resources\Binaries\OpenGL`.
Which is to be executed with:

`dotnet clientogl.dll`

Building with Visual Studio
---------------------------

You can select the desired configuration directly from the solution configurations:

![Screenshot 2022-09-09 235432](https://user-images.githubusercontent.com/25006126/189451063-28418a7b-47f4-47b3-9d8b-512c598284ac.png)

Note that the XNA configurations can only be build with either `x86` or `AnyCPU` (32-bit preferred) to use `Large address aware`.

![Screenshot 2022-09-09 235556](https://user-images.githubusercontent.com/25006126/189451170-d90f665e-19d1-4e6b-a9df-a4994eb143a9.png)

After changing the solution configuration you should exit Visual Studio and reload the solution in order for all the dynamic dependencies to be loaded correctly.
