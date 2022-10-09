# if ($___COMMON__H___) { return }
# $___COMMON__H___ = $true

$RepoRoot = Split-Path $PSScriptRoot -Parent
$ProjectPath = Join-Path $RepoRoot DXMainClient DXMainClient.csproj
$CompiledRoot = Join-Path $RepoRoot Compiled
$EngineMap = @{
  'UniversalGL' = 'OpenGL'
  'WindowsDX'   = 'Windows'
  'WindowsGL'   = 'OpenGL'
  'WindowsXNA'  = 'XNA'
}

function Build-Project($Configuration, $Game, $Engine, $Framework) {
  $Output = Join-Path $CompiledRoot $Game $Framework
  if ($Engine -EQ 'UniversalGL') {
    $Output = Join-Path $Output any
  }
  $Output = Join-Path $Output Resources Binaries ($EngineMap[$Engine])
  dotnet publish $ProjectPath --configuration=$Configuration -property:GAME=$Game -property:ENGINE=$Engine --framework=$Framework --output=$Output
  if ($LASTEXITCODE) { throw }
}