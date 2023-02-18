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

function Build-Project($Configuration, $Game, $Engine, $Framework) {
  $Output = Join-Path $CompiledRoot $Game $Output Resources Binaries ($EngineMap[$Engine])
  if ($Engine -EQ 'WindowsXNA') {
    dotnet publish $ProjectPath -c $Configuration -p:GAME=$Game -p:ENGINE=$Engine -f $Framework -o $Output -p:SatelliteResourceLanguages=en -a x86
  }
  else {
    dotnet publish $ProjectPath -c $Configuration -p:GAME=$Game -p:ENGINE=$Engine -f $Framework -o $Output -p:SatelliteResourceLanguages=en
  }
  if ($LASTEXITCODE) {
    throw "Build failed for $Game $Engine $Framework $Configuration"
  }
}