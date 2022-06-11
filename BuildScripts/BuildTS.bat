dotnet publish ..\DXMainClient\DXMainClient.csproj -c TSDXRelease -f net48 /p:PlatformTarget=AnyCPU
dotnet publish ..\DXMainClient\DXMainClient.csproj --no-self-contained -c TSDXRelease -f net6.0-windows10.0.22000.0 -r win-x86
dotnet publish ..\DXMainClient\DXMainClient.csproj --no-self-contained -c TSDXRelease -f net6.0-windows10.0.22000.0 -r win-x64
dotnet publish ..\DXMainClient\DXMainClient.csproj --no-self-contained -c TSDXRelease -f net6.0-windows10.0.22000.0 -r win-arm64

dotnet publish ..\DXMainClient\DXMainClient.csproj -c TSGLRelease -f net48 /p:PlatformTarget=AnyCPU
dotnet publish ..\DXMainClient\DXMainClient.csproj --no-self-contained -c TSGLRelease -f net6.0-windows10.0.22000.0 -r win-x86
dotnet publish ..\DXMainClient\DXMainClient.csproj --no-self-contained -c TSGLRelease -f net6.0-windows10.0.22000.0 -r win-x64
dotnet publish ..\DXMainClient\DXMainClient.csproj --no-self-contained -c TSGLRelease -f net6.0-windows10.0.22000.0 -r win-arm64

dotnet publish ..\DXMainClient\DXMainClient.csproj -c TSXNARelease -f net48 /p:PlatformTarget=x86
dotnet publish ..\DXMainClient\DXMainClient.csproj --no-self-contained -c TSXNARelease -f net6.0-windows10.0.22000.0 -r win-x86