# CnCNet Client #

The MonoGame / XNA CnCNet client, a platform for playing classic Command & Conquer games and their mods both online and offline. Supports setting up and launching both singleplayer and multiplayer games with [a CnCNet game spawner](https://github.com/CnCNet/ts-patches). Includes an IRC-based chat client with advanced features like private messaging, a friend list, a configurable game lobby, flexible and moddable UI graphics, and extras like game setting configuration and keeping track of match statistics. And much more!

Targets
-------

The primary targets of the client project are


* [Dawn of the Tiberium Age](http://www.moddb.com/mods/the-dawn-of-the-tiberium-age)
* [Twisted Insurrection](http://www.moddb.com/mods/twisted-insurrection)
* [Mental Omega](http://www.moddb.com/mods/mental-omega)
* [CnCNet Yuri's Revenge](https://cncnet.org/yuris-revenge)


However, there is no limitation in the client that would prevent incorporating it into other projects. Any game or mod project that utilizes the CnCNet spawner for Tiberian Sun and Red Alert 2 can be supported. Several other projects also use the client or an unofficial fork of it, including [Tiberian Sun Client](https://www.moddb.com/mods/tiberian-sun-client), [Project Phantom](https://www.moddb.com/mods/project-phantom), [YR Red-Resurrection](https://www.moddb.com/mods/yr-red-resurrection), [The Second Tiberium War](https://www.moddb.com/mods/the-second-tiberium-war) and [CnC: Final War](https://www.moddb.com/mods/cncfinalwar).

Requirements
------------

The client has 2 variants: .NET 4.8 and .NET 6.0.

Each variant has 3 builds: Windows (DirectX11), OpenGL and XNA.
* The Windows and OpenGL builds rely on MonoGame.
* The XNA build relies on Microsoft's XNA Framework 4.0 Refresh.

Building the solution for any platform requires Visual Studio 2022 or newer. A modern version of Visual Studio Code, MonoDevelop or Visual Studio for Mac could also work, but are not officially supported.

Compiling, debugging and usage
------------------------------

* Compiling itself is simple: assuming you have the .NET 6 SDK installed, you can just open the solution with Visual Studio and compile it right away.
* When built as a debug build, the client executable expects to reside in the same directory with the target project's main game executable. Resources should exist in a "Resources" sub-directory in the same directory. The repository contains sample resources and post-build commands for copying them so that you can immediately run the client in debug mode by just hitting the Debug button in Visual Studio.
* When built in release mode, the client executable expects to reside in the "Resources" sub-directory itself. In target projects, the client executables are named `clientdx.exe`, `clientogl.exe` and `clientxna.exe` respectively for each platform.
* The `BuildScripts` directory has automated build scripts that build the client for all 3 platforms and copy the output files to a folder named `Compiled` in the project root. You can then copy the contents of this `Compiled` directory into the `Resources` sub-directory of any target project.

End-user usage
--------------

Windows 7 SP1 or higher is required. The .NET 6.0 MonoGame (DirectX11) build is preferred. The XNA build is intended for those whose GPU does not properly support DX11.

The MonoGame WindowsGL / DesktopGL build is primarily meant for experimental Linux and Mac support.

All builds require either the [.NET 6.0 Desktop Runtime](https://dotnet.microsoft.com/en-us/download/dotnet/6.0/runtime) or the [.NET 4.8 Runtime](https://dotnet.microsoft.com/en-us/download/dotnet-framework/net48).

The XNA build additionally requires the [Microsoft XNA Framework Redistributable 4.0 Refresh](https://www.microsoft.com/en-us/download/details.aspx?id=27598)

Client launcher
---------------

This repository does not contain the client launcher (for example, `DTA.exe` in Dawn of the Tiberium Age) that selects which platform's client executable is most suitable for each user's system. For that, see [dta-mg-client-launcher](https://github.com/CnCNet/dta-mg-client-launcher).

Branches
--------

Currently there are only two major active branches. `develop` is where development happens, and while things should be fairly stable, occasionally there can also be bugs. If you want stability and reliability, the `master` branch is recommended.

Screenshots
-----------

![Screenshot](cncnetchatlobby.png?raw=true "CnCNet IRC Chat Lobby")
![Screenshot](cncnetgamelobby.png?raw=true "CnCNet Game Lobby")
