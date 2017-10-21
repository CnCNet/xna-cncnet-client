# CnCNet Client #
=================

The MonoGame / XNA CnCNet client, a platform for setting up and launching both singleplayer and multiplayer games with a CnCNet game spawner. Includes an IRC-based chat client with advanced features like private messaging and a friend list, a configurable game lobby, flexible and moddable UI graphics, and extras like game setting configuration and keeping track of match statistics. And much more!

Targets
-------

The primary targets of the client project are


* [Dawn of the Tiberium Age](http://www.moddb.com/mods/the-dawn-of-the-tiberium-age)
* [Twisted Insurrection](http://www.moddb.com/mods/twisted-insurrection)
* [Mental Omega](http://www.moddb.com/mods/mental-omega)
* [CnCNet Yuri's Revenge](https://cncnet.org/yuris-revenge)


However, there is no limitation in the client that would prevent incorporating it into other projects. Any game or mod with a CnCNet-style spawner can be supported.

Requirements
------------

The client relies on either MonoGame or Microsoft's XNA Framework, depending on the build platform. Building the solution requires Visual Studio 2017. A modern version of MonoDevelop or Visual Studio for Mac could also work.


* If you're building for Windows (Vista and newer), you can just clone the repository, open the solution with Visual Studio 2015 and compile it right away; the repository includes MonoGame and other necessary references for building for the MonoGame Windows (Vista and newer) platform.
*  For compiling the XNA build, Microsoft XNA Game Studio 4.0 Refresh is needed.


For end-users running Vista or newer Windows, the MonoGame build is preferred. The MonoGame build cannot be run on Windows XP, so the XNA build is intended for XP users.


There is also a MonoGame WindowsGL / DesktopGL build platform option for currently experimental Linux and Mac support. Like with the Windows platform, the repository includes all necessary files and references for compiling the client directly for the WindowsGL / DesktopGL platform.

Screenshots
-----------

![Screenshot](cncnetchatlobby.png?raw=true "CnCNet IRC Chat Lobby")
![Screenshot](cncnetgamelobby.png?raw=true "CnCNet Game Lobby")
