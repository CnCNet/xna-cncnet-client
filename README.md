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

The client has 3 builds: Windows (DirectX11), OpenGL and XNA.
* The Windows and OpenGL builds rely on .NET Framework 4.5 and MonoGame.
* The XNA build relies on .NET Framework 4.0 and Microsoft's XNA Framework 4.0 Refresh.
  * [Installing XNA for Visual Studio 2019](http://flatredball.com/visual-studio-2019-xna-setup/)
  * [Installing XNA for Visual Studio 2017](http://flatredball.com/visual-studio-2017-xna-setup/)

Building the solution for any platform requires Visual Studio 2017 or newer. A modern version of Visual Studio Code, MonoDevelop or Visual Studio for Mac could also work (as well as separate MSBuild without any sort of IDE), but are not officially supported.

Compiling
---------

* Compiling itself is simple: assuming you have the prerequisites installed, you can just open the solution with Visual Studio and compile it right away; the repository includes MonoGame and other necessary DLLs for quick compiling.
* The `BuildScripts` directory has automated build scripts that build the client for all platforms and copy the output files to a folder named `Compiled` in the project root. Note that by default the build scripts also build the XNA version of the client, which requires XNA Framework 4.0 Refresh to be installed. If you don't want to install XNA, then you need to modify `BuildGame.bat` to leave the XNAFramework build out.

Usage
-----

For end-users running Vista or newer Windows, the MonoGame (DirectX11) build is preferred. The MonoGame build cannot be run on Windows XP, so the XNA build is intended for XP users and for those users whose GPUs do not properly support DX11.

The MonoGame WindowsGL / DesktopGL build is primarily meant for experimental Linux and Mac support.

Screenshots
-----------

![Screenshot](cncnetchatlobby.png?raw=true "CnCNet IRC Chat Lobby")
![Screenshot](cncnetgamelobby.png?raw=true "CnCNet Game Lobby")
