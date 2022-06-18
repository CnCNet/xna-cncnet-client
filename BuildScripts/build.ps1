#Requires -Version 5.0

$Script:TargetProjectPath = Join-Path $PSScriptRoot .. DXMainClient
$Script:CompiledPath = Join-Path $PSScriptRoot .. Compiled

$Script:CommonLibraries = @(
  "ClientUpdater"
  "DiscordRPC"
  "Localization"
  "lzo.net"
  "Microsoft.CodeAnalysis.CSharp"
  "Microsoft.CodeAnalysis"
  "Microsoft.CodeAnalysis.VisualBasic"
  "Microsoft.Windows.SDK.NET"
  "Newtonsoft.Json"
  "OpenMcdf"
  "Rampastring.Tools"
  "System.Management"
  "WinRT.Runtime"
)

enum Games {
  Ares
  TS
  YR
}

enum Engines {
  DX
  GL
  XNA
}

enum Configurations {
  Debug
  Release
}

function Build-Project {
  [CmdletBinding()]
  param (
    # Game
    [Parameter(Mandatory)]
    [Games]
    $Game,
    [Parameter(Mandatory)]
    [Engines]
    $Engine,
    [Parameter(Mandatory)]
    [Configurations]
    $Configuration,
    [Parameter(Mandatory)]
    [string]
    $TargetFramework
  )

  begin {
    $Private:TargetFrameworkWithoutTFM = ($TargetFramework -split '-')[0]
    switch ($Engine) {
      DX {
        $Private:SpecialName = 'Windows'
        $Private:ClientSuffix = 'dx'
        break
      }
      GL {
        $Private:SpecialName = 'OpenGL'
        $Private:ClientSuffix = 'ogl'
        break
      }
      XNA {
        $Private:SpecialName = 'XNA'
        $Private:ClientSuffix = 'xna'
        break
      }
    }
  }

  process {
    $dotnetArgs = @(
      "build"
      "$Script:TargetProjectPath"
      "--framework:$TargetFramework"
      "--output:$Script:CompiledPath\$Game\$Private:TargetFrameworkWithoutTFM\Binaries\$Private:SpecialName\"
      "--no-self-contained"
      "--configuration:$Configuration"
      "-p:Engine=$Engine"
      "-p:Game=$Game"
    )
    dotnet $dotnetArgs
    if ($LASTEXITCODE -ne 0) {
      throw "Failed to build project"
    }
  }

  end {
    Get-ChildItem $Script:CompiledPath\$Game\$Private:TargetFrameworkWithoutTFM\Binaries\$Private:SpecialName\DXMainClient.* | ForEach-Object {
      Copy-Item $_.FullName "$Script:CompiledPath\$Game\$Private:TargetFrameworkWithoutTFM\client$Private:ClientSuffix$($_.Extension)"
    }
    Copy-CommonLibraries "$Script:CompiledPath\$Game\$Private:TargetFrameworkWithoutTFM\Binaries\$Private:SpecialName\" ($Engine -eq [Engines]::DX)
  }
}

function Build-Ares {
  [CmdletBinding()]
  param (
    [Parameter(Mandatory)]
    [Configurations]
    $Configuration
  )

  process {
    Build-Project -Configuration $Configuration -Game Ares -Engine DX -TargetFramework net6.0-windows10.0.22000.0
    Build-Project -Configuration $Configuration -Game Ares -Engine GL -TargetFramework net6.0-windows10.0.22000.0

    Build-Project -Configuration $Configuration -Game Ares -Engine DX -TargetFramework net48
    Build-Project -Configuration $Configuration -Game Ares -Engine GL -TargetFramework net48
  }
}

function Build-TS {
  [CmdletBinding()]
  param (
    [Parameter(Mandatory)]
    [Configurations]
    $Configuration
  )

  process {
    Build-Project -Configuration $Configuration -Game TS -Engine DX -TargetFramework net6.0-windows10.0.22000.0
    Build-Project -Configuration $Configuration -Game TS -Engine GL -TargetFramework net6.0-windows10.0.22000.0

    Build-Project -Configuration $Configuration -Game TS -Engine DX -TargetFramework net48
    Build-Project -Configuration $Configuration -Game TS -Engine GL -TargetFramework net48
  }
}

function Build-YR {
  [CmdletBinding()]
  param (
    [Parameter(Mandatory)]
    [Configurations]
    $Configuration
  )

  process {
    Build-Project -Configuration $Configuration -Game YR -Engine DX -TargetFramework net6.0-windows10.0.22000.0
    Build-Project -Configuration $Configuration -Game YR -Engine GL -TargetFramework net6.0-windows10.0.22000.0

    Build-Project -Configuration $Configuration -Game YR -Engine DX -TargetFramework net48
    Build-Project -Configuration $Configuration -Game YR -Engine GL -TargetFramework net48
  }
}

function Build-All {
  [CmdletBinding()]
  param (
    [Parameter(Mandatory)]
    [Configurations]
    $Configuration
  )

  process {
    Build-Ares $Configuration
    Build-TS $Configuration
    Build-YR $Configuration
  }
}

function Copy-CommonLibraries {
  [CmdletBinding()]
  param (
    [Parameter(Mandatory)]
    [string]
    $path,
    [Parameter()]
    [bool]
    $delete = $false
  )

  process {
    if ($delete) {
      Get-ChildItem $path | ForEach-Object {
        if ($CommonLibraries.Contains([System.IO.Path]::GetFileNameWithoutExtension($_))) {
          Remove-Item $_
        }
      }
    }
    else {
      Get-ChildItem $path | ForEach-Object {
        if ($CommonLibraries.Contains([System.IO.Path]::GetFileNameWithoutExtension($_))) {
          Move-Item $_ $path\..
        }
      }
    }
  }

}
