
# CnCNet Client

The MonoGame / XNA CnCNet client, a platform for playing classic Command & Conquer games and their mods both online and offline. Supports setting up and launching both singleplayer and multiplayer games with [a CnCNet game spawner](https://github.com/CnCNet/ts-patches). Includes an IRC-based chat client with advanced features like private messaging, a friend list, a configurable game lobby, flexible and moddable UI graphics, and extras like game setting configuration and keeping track of match statistics. And much more!

## Targets

The primary targets of the client project are
* [Dawn of the Tiberium Age](https://www.moddb.com/mods/the-dawn-of-the-tiberium-age)
* [Twisted Insurrection](https://www.moddb.com/mods/twisted-insurrection)
* [Mental Omega](https://www.moddb.com/mods/mental-omega)
* [CnCNet Yuri's Revenge](https://cncnet.org/yuris-revenge)

However, there is no limitation in the client that would prevent incorporating it into other projects. Any game or mod project that utilizes the CnCNet spawner for Tiberian Sun and Red Alert 2 can be supported. Several other projects also use the client or an unofficial fork of it, including [Tiberian Sun Client](https://www.moddb.com/mods/tiberian-sun-client), [Project Phantom](https://www.moddb.com/mods/project-phantom), [YR Red-Resurrection](https://www.moddb.com/mods/yr-red-resurrection), [The Second Tiberium War](https://www.moddb.com/mods/the-second-tiberium-war) and [CnC: Final War](https://www.moddb.com/mods/cncfinalwar).

## Development requirements

The client has 2 variants: .NET 8.0 and .NET 4.8.
* Both variants have 3 builds: Windows DirectX11, Windows OpenGL and Windows XNA.
* .NET 8.0 in addition has a cross-platform Universal OpenGL build.
* The DirectX11 and OpenGL builds rely on MonoGame.
* The XNA build relies on Microsoft's XNA Framework 4.0 Refresh.

Building the solution for any platform requires Visual Studio 2022 17.8 or newer and/or the .NET SDK 8.0. A modern version of Visual Studio Code, MonoDevelop or Visual Studio for Mac could also work, but are not officially supported.
To debug WindowsXNA builds the .NET SDK 8.0 x86 is additionally required.
When using the included build scripts PowerShell 7.2 or newer is required.

## Compiling and debugging

* Compiling itself is simple: assuming you have the .NET 8.0 SDK installed, you can just open the solution with Visual Studio and compile it right away.
* When built as a debug build, the client executable expects to reside in the same directory with the target project's main game executable. Resources should exist in a "Resources" sub-directory in the same directory. The repository contains sample resources and post-build commands for copying them so that you can immediately run the client in debug mode by just hitting the Debug button in Visual Studio.
* When built in release mode, the client executable expects to reside in the "Resources" sub-directory itself. In target projects, the client libraries are named `clientdx.dll`, `clientogl.dll` and `clientxna.dll` respectively for each platform. For .NET 4.8 these will be `.exe` instead of `.dll` files.
* When built on an OS other than Windows, only the Universal OpenGL build is available.
* The `BuildScripts` directory has automated build scripts that build the client for all platforms and copy the output files to a folder named `Compiled` in the project root. You can then copy the contents of this `Compiled` directory into the `Resources` sub-directory of any target project.

## End-user usage

* Windows: Windows 7 SP1 or higher is required. The DirectX11 build is preferred. The OpenGL or XNA build is intended for those whose GPU does not properly support DX11. On Windows 10/11 the .NET 8.0 variant is recommended, on legacy Windows versions the .NET 4.8 variant is recommended.
* Other OS: Use the Universal OpenGL build.

## End-user requirements

### Windows requirements:

(Optional) The XNA build requires:
* [Microsoft XNA Framework Redistributable 4.0 Refresh](https://www.microsoft.com/en-us/download/details.aspx?id=27598).

#### Windows .NET 8.0 requirements:

* The [.NET 8.0 Desktop Runtime](https://dotnet.microsoft.com/en-us/download/dotnet/8.0/runtime?initial-os=windows) for your specific platform.

(Optional) The XNA build additionally requires:
* [.NET 8.0 Desktop Runtime x86](https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/runtime-desktop-8.0.0-windows-x86-installer).

Windows 7 SP1 and Windows 8.x additionally require:
* Microsoft Visual C++ 2015-2019 Redistributable [64-bit](https://aka.ms/vs/16/release/vc_redist.x64.exe) / [32-bit](https://aka.ms/vs/16/release/vc_redist.x86.exe).

Windows 7 SP1 additionally requires:
* KB3063858 [64-bit](https://www.microsoft.com/download/details.aspx?id=47442) / [32-bit](https://www.microsoft.com/download/details.aspx?id=47409).

#### Windows .NET 4.8 requirements:

* The [.NET Framework 4.8 Runtime](https://dotnet.microsoft.com/en-us/download/dotnet-framework/thank-you/net48-web-installer)

### Linux requirements:

* The [.NET 8.0 Runtime](https://dotnet.microsoft.com/en-us/download/dotnet/8.0/runtime?initial-os=linux) for your specific platform.

### macOS requirements:

* The [.NET 8.0 Runtime](https://dotnet.microsoft.com/en-us/download/dotnet/8.0/runtime?initial-os=macos) for your specific platform.

## Client launcher

This repository does not contain the client launcher (for example, `DTA.exe` in Dawn of the Tiberium Age) that selects which platform's client executable is most suitable for each user's system.
For the .NET 4.8 launcher see [dta-mg-client-launcher](https://github.com/CnCNet/dta-mg-client-launcher).
For the .NET 8.0 launcher see [dta-mg-client-launcher](https://github.com/Rans4ckeR/dta-mg-client-launcher).

## Branches

Currently there are only two major active branches. `develop` is where development happens, and while things should be fairly stable, occasionally there can also be bugs. If you want stability and reliability, the `master` branch is recommended.

## Screenshots

![Screenshot](cncnetchatlobby.png?raw=true "CnCNet IRC Chat Lobby")
![Screenshot](cncnetgamelobby.png?raw=true "CnCNet Game Lobby")
