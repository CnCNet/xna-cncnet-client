dotnet publish ..\DXMainClient\DXMainClient.csproj -c YRDXRelease -f net48 -p:PlatformTarget=AnyCPU || echo ERROR && exit /b
dotnet publish ..\DXMainClient\DXMainClient.csproj --no-self-contained -c YRDXRelease -f net6.0-windows10.0.22000.0 -r win-x86 || echo ERROR && exit /b
dotnet publish ..\DXMainClient\DXMainClient.csproj --no-self-contained -c YRDXRelease -f net6.0-windows10.0.22000.0 -r win-x64 || echo ERROR && exit /b
dotnet publish ..\DXMainClient\DXMainClient.csproj --no-self-contained -c YRDXRelease -f net6.0-windows10.0.22000.0 -r win-arm64 || echo ERROR && exit /b

dotnet publish ..\DXMainClient\DXMainClient.csproj -c YRGLRelease -f net48 -p:PlatformTarget=AnyCPU || echo ERROR && exit /b
dotnet publish ..\DXMainClient\DXMainClient.csproj --no-self-contained -c YRGLRelease -f net6.0-windows10.0.22000.0 -r win-x86 || echo ERROR && exit /b
dotnet publish ..\DXMainClient\DXMainClient.csproj --no-self-contained -c YRGLRelease -f net6.0-windows10.0.22000.0 -r win-x64 || echo ERROR && exit /b
dotnet publish ..\DXMainClient\DXMainClient.csproj --no-self-contained -c YRGLRelease -f net6.0-windows10.0.22000.0 -r win-arm64 || echo ERROR && exit /b

dotnet publish ..\DXMainClient\DXMainClient.csproj -c YRXNARelease -f net48 -p:PlatformTarget=x86 || echo ERROR && exit /b
dotnet publish ..\DXMainClient\DXMainClient.csproj --no-self-contained -c YRXNARelease -f net6.0-windows10.0.22000.0 -r win-x86 || echo ERROR && exit /b