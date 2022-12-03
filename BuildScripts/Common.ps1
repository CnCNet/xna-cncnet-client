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
    dotnet publish $ProjectPath --configuration=$Configuration -property:GAME=$Game -property:ENGINE=$Engine --framework=$Framework --output=$Output --arch=x86
  }
  else {
    dotnet publish $ProjectPath --configuration=$Configuration -property:GAME=$Game -property:ENGINE=$Engine --framework=$Framework --output=$Output
  }
  if ($LASTEXITCODE) {
    throw "Build failed for $Game $Engine $Framework $Configuration"
  }
}