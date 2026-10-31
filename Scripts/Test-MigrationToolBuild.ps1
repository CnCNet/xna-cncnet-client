#!/usr/bin/env pwsh
#Requires -Version 7.2

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path $PSScriptRoot
$configuration = 'UniversalGLRelease'
$project = Join-Path $repoRoot 'MigrationTool/MigrationTool.csproj'
$output = Join-Path $repoRoot 'MigrationTool/bin/Release/UniversalGL/net8.0'
$apphostName = if ($IsWindows) { 'MigrationTool.exe' } else { 'MigrationTool' }
$temporaryOutput = Join-Path ([System.IO.Path]::GetTempPath()) ('migration-tool-build-' + [guid]::NewGuid().ToString('N'))

function Invoke-Dotnet {
    & dotnet @args
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet $args failed with exit code $LASTEXITCODE"
    }
}

function Assert-File([string] $Path) {
    if (!(Test-Path -LiteralPath $Path -PathType Leaf)) {
        throw "Expected file: $Path"
    }
}

function Assert-Help([string] $Executable) {
    $help = & $Executable --help
    if ($LASTEXITCODE -ne 0 -or ($help -join "`n") -notmatch 'CnCNet Client Migration Tool\.') {
        throw "Migration Tool help failed: $Executable"
    }
}

function Assert-Net8Publish([string] $Directory) {
    foreach ($name in @($apphostName, 'MigrationTool.dll', 'MigrationTool.deps.json', 'MigrationTool.runtimeconfig.json', 'ClientCore.dll', 'Rampastring.Tools.dll')) {
        Assert-File (Join-Path $Directory $name)
    }
    Assert-Help (Join-Path $Directory $apphostName)
}

Push-Location $repoRoot
try {
    Invoke-Dotnet restore DXClient.slnx "-p:Configuration=$configuration"
    Invoke-Dotnet build DXClient.slnx "-p:Configuration=$configuration" --no-restore '-m:2'

    # Ordinary builds must keep the apphost, without an extra staging directory.
    Assert-File (Join-Path $output $apphostName)
    if (Test-Path -LiteralPath (Join-Path $output 'MigrationTool') -PathType Container) {
        throw 'An ordinary build created a MigrationTool staging directory beside the executable.'
    }

    New-Item -ItemType Directory -Path $temporaryOutput | Out-Null
    Push-Location $temporaryOutput
    try {
        Assert-Help (Join-Path $output $apphostName)

        Invoke-Dotnet publish $project "-p:Configuration=$configuration" -f net8.0 --no-restore
        Assert-Net8Publish (Join-Path $output 'publish')

        # Publishing must honor the requested destination without staging beside it.
        $customOutput = Join-Path $temporaryOutput 'custom'
        Invoke-Dotnet publish $project "-p:Configuration=$configuration" -f net8.0 --no-restore -o $customOutput
        Assert-Net8Publish $customOutput
        if (Test-Path -LiteralPath (Join-Path $temporaryOutput 'MigrationTool')) {
            throw 'Publishing wrote an unexpected sibling MigrationTool directory.'
        }

        # Match the separate .NET Framework package produced by Build.ps1.
        $net48Output = Join-Path $temporaryOutput 'net48'
        Invoke-Dotnet publish $project --graph -f net48 -o $net48Output '-p:SatelliteResourceLanguages=en'
        foreach ($name in @('MigrationTool.exe', 'MigrationTool.exe.config', 'ClientCore.dll', 'Rampastring.Tools.dll')) {
            Assert-File (Join-Path $net48Output $name)
        }
        if ($IsWindows) {
            Assert-Help (Join-Path $net48Output 'MigrationTool.exe')
        }
        if (Test-Path -LiteralPath (Join-Path $temporaryOutput 'MigrationTool')) {
            throw 'The .NET Framework publish wrote an unexpected sibling MigrationTool directory.'
        }

        # The target runtime, rather than the build host, determines the apphost.
        $crossRuntime = if ($IsWindows) { 'linux-x64' } else { 'win-x64' }
        $crossApphostName = if ($IsWindows) { 'MigrationTool' } else { 'MigrationTool.exe' }
        $crossOutput = Join-Path $temporaryOutput $crossRuntime
        Invoke-Dotnet publish $project "-p:Configuration=$configuration" -f net8.0 -r $crossRuntime --self-contained false -o $crossOutput
        $crossApphost = Join-Path $crossOutput $crossApphostName
        Assert-File $crossApphost
        $header = [System.IO.File]::ReadAllBytes($crossApphost)
        $expectedHeader = if ($IsWindows) { '7F-45-4C-46' } else { '4D-5A' }
        $headerLength = if ($IsWindows) { 4 } else { 2 }
        if ([System.BitConverter]::ToString($header, 0, $headerLength) -ne $expectedHeader) {
            throw "Unexpected apphost format for $crossRuntime"
        }
    }
    finally {
        Pop-Location
    }

    Write-Output 'Migration Tool build and publish checks passed.'
}
finally {
    Pop-Location
    if (Test-Path -LiteralPath $temporaryOutput) {
        Remove-Item -LiteralPath $temporaryOutput -Recurse -Force
    }
}
