# CnCNet Client

The MonoGame / XNA CnCNet client, a platform for playing classic Command & Conquer games and their mods both online and offline. Supports setting up and launching both singleplayer and multiplayer games with [a CnCNet game spawner](https://github.com/CnCNet/ts-patches). Includes an IRC-based chat client with advanced features like private messaging, a friend list, a configurable game lobby, flexible and moddable UI graphics, and extras like game setting configuration and keeping track of match statistics. And much more!

You can find the [dedicated project development chat](https://discord.gg/M5gGdBYG5m) at C&C Mod Haven Discord server.

## Targets

The primary targets of the client project are
* [Dawn of the Tiberium Age](https://www.moddb.com/mods/the-dawn-of-the-tiberium-age)
* [Twisted Insurrection](https://www.moddb.com/mods/twisted-insurrection)
* [Mental Omega](https://www.moddb.com/mods/mental-omega)
* [CnCNet Yuri's Revenge](https://cncnet.org/yuris-revenge)

However, there is no limitation in the client that would prevent incorporating it into other projects. Any game or mod project that utilizes the CnCNet spawner for Tiberian Sun and Red Alert 2 can be supported. Several other projects also use the client or an unofficial fork of it, including [Tiberian Sun Client](https://www.moddb.com/mods/tiberian-sun-client), [Project Phantom](https://www.moddb.com/mods/project-phantom), [YR Red-Resurrection](https://www.moddb.com/mods/yr-red-resurrection), [The Second Tiberium War](https://www.moddb.com/mods/the-second-tiberium-war) and [CnC: Final War](https://www.moddb.com/mods/cncfinalwar).

## Development requirements

The client supports 2 runtimes: .NET 4.8 and .NET 8.0.
* Both runtimes have 3 rendering engines: Windows DirectX11, Windows OpenGL and Windows XNA.
* .NET 8.0 in addition has a cross-platform Universal OpenGL engine.
* The DirectX11 and OpenGL engines rely on MonoGame.
* The XNA engine relies on Microsoft's XNA Framework 4.0 Refresh.

To build the client, **you must use Git to clone the repository**, instead of downloading a ZIP archive. After cloning, make sure to **initialize and update the submodules** using the following command:
```shell
git submodule update --init --recursive
```

Building for **any** platform requires the .NET SDK 10.0. Editing the source code requires Visual Studio 2026 or newer, or Rider 2025.3 or newer. A modern version of Visual Studio Code also works, but is not officially supported.
To debug WindowsXNA builds the .NET SDK 10.0 x86 is additionally required.
When using the included build scripts, [PowerShell 7](https://learn.microsoft.com/powershell/scripting/install/installing-powershell-on-windows) is required.

## Building and debugging

* It is simple to build the client. Assuming you have the .NET SDK 10.0 and PowerShell 7 installed, you can just double-click `Scripts/build.bat` to compile it right away. You can then copy the contents of this `Compiled` directory into the `Resources` sub-directory of any target project. Please turn off Visual Studio while executing scripts.

* If you want to run the client in debug mode, open the solution file `DXClient.slnx` using Visual Studio, and select Debug -> Start Debugging (F5).

> [!IMPORTANT]
> If you switch among different solution configurations in Visual Studio (e.g. switch to `UniversalGLRelease` from `WindowsDXDebug`), especially switching between .NET 4.8 and .NET 8.0 runtimes, **it is highly recommended to restart Visual Studio after switching configurations to prevent unexpected error messages**. If restarting Visual Studio do not work as intended, try deleting all `obj` folders in each project by running `ClearObjDirs.bat` in `Scripts` folder. Due to the same reason, **it is also highly recommended to close Visual Studio when using any scripts in `Scripts` folder**.

### Advanced notes on building and debugging

* When built as a debug build, the client executable expects to reside in the same directory with the target project's main game executable. Resources should exist in a "Resources" sub-directory in the same directory. The repository contains sample resources and post-build commands for copying them so that you can immediately run the client in debug mode by just hitting the Debug button in Visual Studio.

* When built in release mode, the client executables expect to reside in the `Resources` sub-directory itself for .NET 4.8, named `clientdx.exe`, `clientogl.exe` and `clientxna.exe`. Each `.exe` file or `.dll` file expects a `.pdb` file for diagnostics purpose. It's advised not to delete these `.pdb` files. Keep all `.pdb` files even for end users.

* For .NET 8, When built in release mode, the client executables expect to reside in `Resources/BinariesNET8/{Windows, OpenGL, UniversalGL, XNA}` folders, named `client{dx, ogl, ogl, xna}.dll`, respectively. Note that `client{dx, ogl, ogl, xna}.runtimeconfig.json` files are required for the corresponding .NET 8 DLLs. When built on an OS other than Windows, only the Universal OpenGL engine is available.

* Some dependencies are stored in `References` folder instead of the official NuGet source. This folder is also useful if you are working on modifying a dependency and debugging in your local machine without publishing the modification to NuGet. However, if you have replaced the `.(s)nupkg` files of a package, without altering the package version, be sure to remove the corresponding package from `%USERPROFILE%\.nuget\packages` folder (Windows) to purge the old version. 

Refer to [Docs/Build.md](/Docs/Build.md) for more information about building the client.

## End-user requirements

* Windows: Windows 7 SP1 or higher is required. The preferred rendering engine is DirectX11 (.NET 4.8), i.e., `clientdx.exe`. If your GPU does not support DX11, consider using the OpenGL or XNA engine instead. Advanced users may experiment with .NET 8 runtime at their discretion.

* Other OS: Use the Universal OpenGL engine.

### Windows .NET 4.8 requirements:

* The [.NET Framework 4.8 Runtime](https://dotnet.microsoft.com/en-us/download/dotnet-framework/thank-you/net48-web-installer)

(Optional) The XNA engine requires:
* [Microsoft XNA Framework Redistributable 4.0 Refresh](https://www.microsoft.com/en-us/download/details.aspx?id=27598).

### Linux requirements:

* The [.NET 8.0 Runtime](https://dotnet.microsoft.com/en-us/download/dotnet/8.0/runtime?initial-os=linux) for your specific platform.

### macOS requirements:

* The [.NET 8.0 Runtime](https://dotnet.microsoft.com/en-us/download/dotnet/8.0/runtime?initial-os=macos) for your specific platform.

### Windows .NET 8.0 requirements:

<details>
  <summary>Windows .NET 8.0 requirements</summary>

* The [.NET 8.0 Desktop Runtime](https://dotnet.microsoft.com/en-us/download/dotnet/8.0/runtime?initial-os=windows) for your specific platform.

(Optional) The XNA engine requires:
* [Microsoft XNA Framework Redistributable 4.0 Refresh](https://www.microsoft.com/en-us/download/details.aspx?id=27598).
* [.NET 8.0 Desktop Runtime x86](https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/runtime-desktop-8.0.0-windows-x86-installer).

Windows 7 SP1 and Windows 8.x additionally require:
* Microsoft Visual C++ 2015-2019 Redistributable [64-bit](https://aka.ms/vs/16/release/vc_redist.x64.exe) / [32-bit](https://aka.ms/vs/16/release/vc_redist.x86.exe). Note: the latest version of this redistributable is named "Microsoft Visual C++ 2015-2026 Redistributable", available [here](https://learn.microsoft.com/cpp/windows/latest-supported-vc-redist). We recommend using the latest version instead of the 2015-2019 version.

Windows 7 SP1 additionally requires:
* KB3063858 [64-bit](https://www.microsoft.com/download/details.aspx?id=47442) / [32-bit](https://www.microsoft.com/download/details.aspx?id=47409).
</details>

## Client launcher

This repository does not contain the client launcher (for example, `DTA.exe` in Dawn of the Tiberium Age) that selects which platform's client executable is most suitable for each user's system.
See [xna-cncnet-client-launcher](https://github.com/CnCNet/xna-cncnet-client-launcher).

## Branches

Currently there are only two major active branches. `develop` is where development happens, and while things should be fairly stable, occasionally there can also be bugs. If you want stability and reliability, the `master` branch is recommended.

## Screenshots

![Screenshot](cncnetchatlobby.png?raw=true "CnCNet IRC Chat Lobby")
![Screenshot](cncnetgamelobby.png?raw=true "CnCNet Game Lobby")

## License

CnCNet Client
Copyright (C) 2013-2026 CnCNet, Rampastring

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program.  If not, see <https://www.gnu.org/licenses/>.

### Additional permission under GNU GPL version 3 section 7

If you modify this program, or any covered work, by linking or combining it with the Steamworks SDK (or a modified version of that library), containing parts covered by the terms of the Steamworks SDK's license, the licensors of this program grant you additional permission to convey the resulting work.

## Sponsored by

<a href="https://www.digitalocean.com/?refcode=337544e2ec7b&utm_campaign=Referral_Invite&utm_medium=opensource&utm_source=CnCNet" title="Powered by Digital Ocean" target="_blank">
    <img src="https://opensource.nyc3.cdn.digitaloceanspaces.com/attribution/assets/PoweredByDO/DO_Powered_by_Badge_blue.svg" width="201px" alt="Powered By Digital Ocean" />
</a>
