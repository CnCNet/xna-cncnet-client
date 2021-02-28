#/bin/sh
set -x

function build()
{
    platform=$1
    configuration=$2

    xbuild ../DXClient.sln /t:Rebuild /p:Platform=$platform /p:Configuration=$configuration
}

function copyCompiled()
{
    arg=$1

    mkdir -p ../Compiled${arg}/Binaries/OpenGL/x86
    mkdir -p ../Compiled${arg}/Binaries/Windows
    mkdir -p ../Compiled${arg}/Binaries/XNA

    cp ../DXMainClient/bin/Windows/Release/DTAClient.exe ../Compiled${arg}/clientdx.exe
    cp ../DXMainClient/bin/Windows/Release/ClientCore.dll ../Compiled${arg}/Binaries/Windows/
    cp ../DXMainClient/bin/Windows/Release/ClientGUI.dll ../Compiled${arg}/Binaries/Windows/
    cp ../DXMainClient/bin/Windows/Release/DTAConfig.dll ../Compiled${arg}/Binaries/Windows/

    cp ../DXMainClient/bin/Windows/Release/Rampastring.XNAUI.dll ../Compiled${arg}/Binaries/Windows/
    cp ../DXMainClient/bin/Windows/Release/MonoGame.Framework.dll ../Compiled${arg}/Binaries/Windows/MonoGame.Framework.dll
    cp ../DXMainClient/bin/Windows/Release/SharpDX.Direct2D1.dll ../Compiled${arg}/Binaries/Windows/SharpDX.Direct2D1.dll
    cp ../DXMainClient/bin/Windows/Release/SharpDX.Direct3D11.dll ../Compiled${arg}/Binaries/Windows/SharpDX.Direct3D11.dll
    cp ../DXMainClient/bin/Windows/Release/SharpDX.dll ../Compiled${arg}/Binaries/Windows/SharpDX.dll
    cp ../DXMainClient/bin/Windows/Release/SharpDX.DXGI.dll ../Compiled${arg}/Binaries/Windows/SharpDX.DXGI.dll
    cp ../DXMainClient/bin/Windows/Release/SharpDX.MediaFoundation.dll ../Compiled${arg}/Binaries/Windows/SharpDX.MediaFoundation.dll
    cp ../DXMainClient/bin/Windows/Release/SharpDX.XAudio2.dll ../Compiled${arg}/Binaries/Windows/SharpDX.XAudio2.dll
    cp ../DXMainClient/bin/Windows/Release/SharpDX.XInput.dll ../Compiled${arg}/Binaries/Windows/SharpDX.XInput.dll

    cp ../DXMainClient/bin/WindowsGL/Release/DTAClient.exe ../Compiled${arg}/clientogl.exe
    cp ../DXMainClient/bin/WindowsGL/Release/ClientCore.dll ../Compiled${arg}/Binaries/OpenGL/
    cp ../DXMainClient/bin/WindowsGL/Release/ClientGUI.dll ../Compiled${arg}/Binaries/OpenGL/
    cp ../DXMainClient/bin/WindowsGL/Release/DTAConfig.dll ../Compiled${arg}/Binaries/OpenGL/
    cp ../DXMainClient/bin/WindowsGL/Release/MonoGame.Framework.dll ../Compiled${arg}/Binaries/OpenGL/
    cp ../DXMainClient/bin/WindowsGL/Release/Rampastring.XNAUI.dll ../Compiled${arg}/Binaries/OpenGL/
    cp ../References/WindowsGL/x86/SDL2.dll ../Compiled${arg}/Binaries/OpenGL/x86/
	cp ../References/WindowsGL/x86/soft_oal.dll ../Compiled${arg}/Binaries/OpenGL/x86/

    cp ../DXMainClient/bin/XNAFramework/Release/DTAClient.exe ../Compiled${arg}/clientxna.exe
    cp ../DXMainClient/bin/XNAFramework/Release/ClientCore.dll ../Compiled${arg}/Binaries/XNA/
    cp ../DXMainClient/bin/XNAFramework/Release/ClientGUI.dll ../Compiled${arg}/Binaries/XNA/
    cp ../DXMainClient/bin/XNAFramework/Release/DTAConfig.dll ../Compiled${arg}/Binaries/XNA/
    cp ../DXMainClient/bin/XNAFramework/Release/Rampastring.XNAUI.dll ../Compiled${arg}/Binaries/XNA/

    cp ../References/DTAUpdater.dll ../Compiled${arg}/Binaries/
    cp ../References/Ionic.Zip.dll ../Compiled${arg}/Binaries/
    cp ../References/MapThumbnailExtractor.dll ../Compiled${arg}/Binaries/
    cp ../References/Rampastring.Tools.dll ../Compiled${arg}/Binaries/
    cp ../References/DiscordRPC.dll ../Compiled${arg}/Binaries/
}


case $1 in
    DTA)
        build SharpDX DTARelease
        build WindowsGL DTARelease
        build XNAFramework DTARelease
        copyCompiled DTA
        ;;
    TI)
        build SharpDX TIRelease
        build WindowsGL TIRelease
        build XNAFramework TIRelease
        copyCompiled TI
        ;;
    YR)
        build SharpDX YRRelease
        build WindowsGL YRRelease
        build XNAFramework YRRelease
        copyCompiled YR
        ;;
    MO)
        build SharpDX MORelease
        build WindowsGL MORelease
        build XNAFramework MORelease
        copyCompiled MO
        ;;
esac
