# Build & Publish #

The information below describes the steps that the default build script (build.ps1) performs.

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
* net8.0

WindowsDX, WindowsGL & WindowsXNA:
* net48

Overview of the Engine configurations differences:

| | OS Support | Default Platform | Technology |
| - | ---------- | -------- | ---------- |
| UniversalGL | Any | AnyCPU | MonoGame DesktopGL |
| WindowsDX | Windows | AnyCPU | MonoGame WindowsDX + WinForms |
| WindowsGL | Windows | AnyCPU | MonoGame DesktopGL + WinForms |
| WindowsXNA | Windows | x86 | Microsoft XNA + WinForms |

Build output
------------

The build output when using the `dotnet publish` command is created in `\Compiled`.

Launching the client is done by running e.g.:

`dotnet clientogl.dll`

Building with Visual Studio
---------------------------

You can select the desired configuration directly from the solution configurations:

![Screenshot 2022-09-09 235432](https://user-images.githubusercontent.com/25006126/189451063-28418a7b-47f4-47b3-9d8b-512c598284ac.png)

Note that the XNA configurations can only be built/debugged with `x86`.

![Screenshot 2022-09-09 235556](https://user-images.githubusercontent.com/25006126/189451170-d90f665e-19d1-4e6b-a9df-a4994eb143a9.png)

After changing the solution configuration you should exit Visual Studio and reload the solution in order for all the dynamic dependencies to be loaded correctly.