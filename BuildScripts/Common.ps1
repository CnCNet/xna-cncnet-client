# if ($___COMMON__H___) { return }
# $___COMMON__H___ = $true

$RepoRoot = Split-Path $PSScriptRoot -Parent
$ProjectPath = Join-Path $RepoRoot DXMainClient DXMainClient.csproj
$CompiledRoot = Join-Path $RepoRoot Compiled
$EngineMap = @{
  'UniversalGL' = 'UniversalGL'
  'WindowsDX'   = 'Windows'
  'WindowsGL'   = 'OpenGL'
  'WindowsXNA'  = 'XNA'
}

function Build-Project($Configuration, $Game, $Engine, $Framework, $AssemblySemVer, $AssemblySemFileVer, $InformationalVersion) {
  $Output = Join-Path $CompiledRoot $Game ($Framework -Split "-")[0] $Output Resources Binaries ($EngineMap[$Engine])
  dotnet publish $ProjectPath -c $Configuration -p:GAME=$Game -p:ENGINE=$Engine -f $Framework -o $Output -p:SatelliteResourceLanguages=en -p:AssemblyVersion=$AssemblySemVer -p:FileVersion=$AssemblySemFileVer -p:InformationalVersion=$InformationalVersion $($Engine -EQ 'WindowsXNA' ? '--arch=x86' : '')

  if ($LASTEXITCODE) {
    throw "Build failed for $Game $Engine $Framework $Configuration"
  }
}