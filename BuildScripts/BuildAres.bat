dotnet publish ..\DXMainClient\DXMainClient.csproj -c AresDXRelease -f net48 -p:PlatformTarget=AnyCPU
dotnet publish ..\DXMainClient\DXMainClient.csproj --no-self-contained -c AresDXRelease -f net6.0-windows10.0.22000.0 -r win-x86
dotnet publish ..\DXMainClient\DXMainClient.csproj --no-self-contained -c AresDXRelease -f net6.0-windows10.0.22000.0 -r win-x64
dotnet publish ..\DXMainClient\DXMainClient.csproj --no-self-contained -c AresDXRelease -f net6.0-windows10.0.22000.0 -r win-arm64

dotnet publish ..\DXMainClient\DXMainClient.csproj -c AresGLRelease -f net48 -p:PlatformTarget=AnyCPU
dotnet publish ..\DXMainClient\DXMainClient.csproj --no-self-contained -c AresGLRelease -f net6.0-windows10.0.22000.0 -r win-x86
dotnet publish ..\DXMainClient\DXMainClient.csproj --no-self-contained -c AresGLRelease -f net6.0-windows10.0.22000.0 -r win-x64
dotnet publish ..\DXMainClient\DXMainClient.csproj --no-self-contained -c AresGLRelease -f net6.0-windows10.0.22000.0 -r win-arm64

dotnet publish ..\DXMainClient\DXMainClient.csproj -c AresXNARelease -f net48 -p:PlatformTarget=x86
dotnet publish ..\DXMainClient\DXMainClient.csproj --no-self-contained -c AresXNARelease -f net6.0-windows10.0.22000.0 -r win-x86