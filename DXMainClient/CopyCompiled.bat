@echo off
echo Taking binaries compiled using Visual Studio and putting them into correct folders with the correct names for use in target projects.
echo(
cd bin
rd /S CompiledResources
mkdir CompiledResources
cd CompiledResources
mkdir Binaries
cd Binaries
mkdir OpenGL
mkdir Windows
mkdir XNA

REM Setting up paths

set cr="..\..\CompiledResources\"
set commonBinaries="..\..\CompiledResources\Binaries\"
set winBinaries="..\..\CompiledResources\Binaries\Windows\"
set oglBinaries="..\..\CompiledResources\Binaries\OpenGL\"
set xnaBinaries="..\..\CompiledResources\Binaries\XNA\"

echo(
echo Windows

cd ..\..\Windows\Release\
copy DTAClient.exe %cr%clientdx.exe
copy ClientCore.dll %winBinaries%ClientCore.dll
copy ClientGUI.dll %winBinaries%ClientGUI.dll
copy DTAConfig.dll %winBinaries%DTAConfig.dll
copy Rampastring.XNAUI.dll %winBinaries%Rampastring.XNAUI.dll
copy MonoGame.Framework.dll %winBinaries%MonoGame.Framework.dll
copy SharpDX.Direct2D1.dll %winBinaries%SharpDX.Direct2D1.dll
copy SharpDX.Direct3D9.dll %winBinaries%SharpDX.Direct3D9.dll
copy SharpDX.Direct3D11.dll %winBinaries%SharpDX.Direct3D11.dll
copy SharpDX.dll %winBinaries%SharpDX.dll
copy SharpDX.DXGI.dll %winBinaries%SharpDX.DXGI.dll
copy SharpDX.MediaFoundation.dll %winBinaries%SharpDX.MediaFoundation.dll
copy SharpDX.RawInput.dll %winBinaries%SharpDX.RawInput.dll
copy SharpDX.XAudio2.dll %winBinaries%SharpDX.XAudio2.dll
copy SharpDX.XInput.dll %winBinaries%SharpDX.XInput.dll

echo OpenGL

cd ..\..\WindowsGL\Release\
copy DTAClient.exe %cr%clientogl.exe
copy ClientCore.dll %oglBinaries%ClientCore.dll
copy ClientGUI.dll %oglBinaries%ClientGUI.dll
copy DTAConfig.dll %oglBinaries%DTAConfig.dll
copy MonoGame.Framework.dll %oglBinaries%MonoGame.Framework.dll
copy NVorbis.dll %oglBinaries%NVorbis.dll
copy OpenTK.dll %oglBinaries%OpenTK.dll
copy Rampastring.XNAUI.dll %oglBinaries%Rampastring.XNAUI.dll

echo XNA

cd ..\..\XNAFramework\Release\
copy DTAClient.exe %cr%clientxna.exe
copy ClientCore.dll %xnaBinaries%ClientCore.dll
copy ClientGUI.dll %xnaBinaries%ClientGUI.dll
copy DTAConfig.dll %xnaBinaries%DTAConfig.dll
copy Rampastring.XNAUI.dll %xnaBinaries%Rampastring.XNAUI.dll

echo Common

copy DTAUpdater.dll %commonBinaries%DTAUpdater.dll
copy Ionic.Zip.dll %commonBinaries%Ionic.Zip.dll
copy MapThumbnailExtractor.dll %commonBinaries%MapThumbnailExtractor.dll
copy Rampastring.Tools.dll %commonBinaries%Rampastring.Tools.dll

echo(
echo Copying complete.
pause
