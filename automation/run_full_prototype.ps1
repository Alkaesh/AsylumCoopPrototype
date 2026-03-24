param(
    [string]$ProjectPath = "",
    [string]$BuildExe = "1",
    [string]$OutputExePath = ""
)

$ErrorActionPreference = "Stop"

function ConvertTo-BoolSafe {
    param([string]$Value)

    if ([string]::IsNullOrWhiteSpace($Value)) {
        return $false
    }

    switch ($Value.Trim().ToLowerInvariant()) {
        "1" { return $true }
        "0" { return $false }
        "true" { return $true }
        "false" { return $false }
        "yes" { return $true }
        "no" { return $false }
        "on" { return $true }
        "off" { return $false }
        default {
            throw "Invalid BuildExe value '$Value'. Use 1/0, true/false, yes/no."
        }
    }
}

function Write-Info {
    param([string]$Message)
    Write-Host "[Horror-AutoSetup] $Message" -ForegroundColor Cyan
}

function Stop-UnityInstancesForProject {
    param([string]$TargetProjectPath)

    if ([string]::IsNullOrWhiteSpace($TargetProjectPath)) {
        return
    }

    $normalizedPath = $TargetProjectPath.ToLowerInvariant().Replace("\", "/")
    $unityProcesses = Get-CimInstance Win32_Process -Filter "Name='Unity.exe'" -ErrorAction SilentlyContinue
    if ($null -eq $unityProcesses) {
        return
    }

    $toStop = @()
    foreach ($process in $unityProcesses) {
        $commandLine = [string]$process.CommandLine
        if ([string]::IsNullOrWhiteSpace($commandLine)) {
            continue
        }

        $normalizedCmd = $commandLine.ToLowerInvariant().Replace("\", "/")
        if ($normalizedCmd -like "*$normalizedPath*") {
            $toStop += $process
        }
    }

    if ($toStop.Count -eq 0) {
        return
    }

    Write-Info "Closing existing Unity process for this project..."
    foreach ($process in $toStop) {
        Stop-Process -Id $process.ProcessId -Force -ErrorAction SilentlyContinue
    }
    Start-Sleep -Seconds 2
}

function Test-GitAvailable {
    $gitCmd = Get-Command git -ErrorAction SilentlyContinue
    return ($null -ne $gitCmd)
}

function Copy-DirectorySafe {
    param(
        [string]$Source,
        [string]$Destination
    )

    if (-not (Test-Path $Source)) {
        throw "Source directory not found: $Source"
    }

    if (Test-Path $Destination) {
        Remove-Item -LiteralPath $Destination -Recurse -Force -ErrorAction SilentlyContinue
    }

    New-Item -ItemType Directory -Path (Split-Path -Path $Destination -Parent) -Force | Out-Null
    Copy-Item -Path $Source -Destination $Destination -Recurse -Force
}

function Write-TextNoBom {
    param(
        [string]$Path,
        [string]$Content
    )

    $utf8NoBom = New-Object System.Text.UTF8Encoding($false)
    [System.IO.File]::WriteAllText($Path, $Content, $utf8NoBom)
}

function Ensure-MirrorSourceWithoutGit {
    param([string]$TargetProjectPath)

    $markerCore = Join-Path $TargetProjectPath "Assets\Mirror\Core\NetworkBehaviour.cs"
    $markerRuntime = Join-Path $TargetProjectPath "Assets\Mirror\Runtime\NetworkBehaviour.cs"
    if ((Test-Path $markerCore) -or (Test-Path $markerRuntime)) {
        Write-Info "Mirror source already present in Assets/Mirror."
        return
    }

    $tmpRoot = Join-Path $TargetProjectPath "automation\_mirror_tmp"
    $zipPath = Join-Path $tmpRoot "mirror.zip"
    $extractPath = Join-Path $tmpRoot "extract"

    if (Test-Path $tmpRoot) {
        Remove-Item -LiteralPath $tmpRoot -Recurse -Force -ErrorAction SilentlyContinue
    }
    New-Item -ItemType Directory -Path $tmpRoot -Force | Out-Null

    $url = "https://codeload.github.com/MirrorNetworking/Mirror/zip/refs/heads/master"
    Write-Info "Git not found. Downloading Mirror source zip..."
    $oldProgress = $ProgressPreference
    $ProgressPreference = "SilentlyContinue"
    try {
        Invoke-WebRequest -Uri $url -OutFile $zipPath -UseBasicParsing
    }
    finally {
        $ProgressPreference = $oldProgress
    }

    Write-Info "Extracting Mirror source..."
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    [System.IO.Directory]::CreateDirectory($extractPath) | Out-Null
    [System.IO.Compression.ZipFile]::ExtractToDirectory($zipPath, $extractPath)

    $candidate = $null
    $networkBehaviourFiles = Get-ChildItem -Path $extractPath -Recurse -Filter "NetworkBehaviour.cs" -File -ErrorAction SilentlyContinue
    foreach ($file in $networkBehaviourFiles) {
        $parent = Split-Path -Path $file.FullName -Parent
        $leaf = Split-Path -Path $parent -Leaf
        if ($leaf -in @("Core", "Runtime")) {
            $candidate = Split-Path -Path $parent -Parent
            break
        }
    }

    if (-not $candidate -or -not (Test-Path $candidate)) {
        $sample = (Get-ChildItem -Path $extractPath -Recurse -File -ErrorAction SilentlyContinue | Select-Object -First 25 | ForEach-Object { $_.FullName }) -join "`n"
        throw "Mirror source directory not found inside downloaded archive. Sample files:`n$sample"
    }

    $targetMirror = Join-Path $TargetProjectPath "Assets\Mirror"
    Write-Info "Installing Mirror source to Assets/Mirror..."
    Copy-DirectorySafe -Source $candidate -Destination $targetMirror

    Remove-Item -LiteralPath $tmpRoot -Recurse -Force -ErrorAction SilentlyContinue
}

function Find-UnityEditor {
    if ($env:UNITY_EXE -and (Test-Path $env:UNITY_EXE)) {
        return (Resolve-Path $env:UNITY_EXE).Path
    }

    $candidates = New-Object System.Collections.Generic.List[string]

    $hubEditors = Join-Path ${env:ProgramFiles} "Unity\Hub\Editor"
    if (Test-Path $hubEditors) {
        $unityFromHub = Get-ChildItem -Path $hubEditors -Directory -ErrorAction SilentlyContinue |
            Sort-Object Name -Descending |
            ForEach-Object { Join-Path $_.FullName "Editor\Unity.exe" } |
            Where-Object { Test-Path $_ }

        foreach ($item in $unityFromHub) {
            $candidates.Add($item)
        }
    }

    $legacyUnity = Join-Path ${env:ProgramFiles} "Unity\Editor\Unity.exe"
    if (Test-Path $legacyUnity) {
        $candidates.Add($legacyUnity)
    }

    if ($candidates.Count -gt 0) {
        return $candidates[0]
    }

    throw "Unity Editor not found. Install Unity Hub + Unity 6 or set UNITY_EXE environment variable."
}

function Invoke-UnityBatch {
    param(
        [string]$UnityExe,
        [string[]]$UnityArgs,
        [string]$LogPath,
        [string]$ProjectPathForLockHandling = "",
        [int]$MaxAttempts = 2,
        [bool]$AllowHealthyNonZero = $true
    )

    function Test-UnityLogHasFatalMarkers {
        param([string]$Path)

        if (-not (Test-Path $Path)) {
            return $true
        }

        $fatalPatterns = @(
            "Scripts have compiler errors",
            "Exiting without the bug reporter",
            "error CS[0-9]{4}",
            "ILPostProcessorHook: .*error",
            "Unhandled Exception",
            "Aborting batchmode due failure",
            "another Unity instance is running with this project open",
            "BuildFailedException",
            "Fatal Error"
        )

        foreach ($pattern in $fatalPatterns) {
            if (Select-String -Path $Path -Pattern $pattern -Quiet) {
                return $true
            }
        }

        return $false
    }

    for ($attempt = 1; $attempt -le $MaxAttempts; $attempt++) {
        Remove-Item -LiteralPath $LogPath -Force -ErrorAction SilentlyContinue
        Write-Info ("Unity args: " + ($UnityArgs -join " "))
        $allArgs = @($UnityArgs + @("-logFile", $LogPath))
        $process = Start-Process -FilePath $UnityExe -ArgumentList $allArgs -NoNewWindow -Wait -PassThru
        $exitCode = $process.ExitCode

        if ($exitCode -eq 0) {
            return
        }

        $hasFatalMarkers = Test-UnityLogHasFatalMarkers -Path $LogPath
        if ($AllowHealthyNonZero -and (-not $hasFatalMarkers)) {
            Write-Info "Unity exited with code $exitCode but no fatal markers found in log. Continuing."
            return
        }

        if ($attempt -lt $MaxAttempts) {
            Write-Info "Unity batch failed (attempt $attempt/$MaxAttempts, code $exitCode). Retrying..."
            if (Select-String -Path $LogPath -Pattern "another Unity instance is running with this project open" -Quiet) {
                Stop-UnityInstancesForProject -TargetProjectPath $ProjectPathForLockHandling
            }
            Start-Sleep -Seconds 2
            continue
        }

        throw "Unity batch command failed. See log: $LogPath"
    }
}

function Ensure-UnityProject {
    param(
        [string]$UnityExe,
        [string]$TargetProjectPath
    )

    $projectVersionPath = Join-Path $TargetProjectPath "ProjectSettings\ProjectVersion.txt"
    if (Test-Path $projectVersionPath) {
        Write-Info "Unity project already initialized."
        return
    }

    Write-Info "Initializing Unity project files..."
    $createLog = Join-Path $TargetProjectPath "automation\unity_create_project.log"

    & $UnityExe @(
        "-batchmode",
        "-nographics",
        "-quit",
        "-projectPath", $TargetProjectPath
    ) -logFile $createLog

    if ($LASTEXITCODE -ne 0) {
        if (Test-Path $projectVersionPath) {
            Write-Info "Unity returned non-zero during initial bootstrap, but project files were created. Continuing..."
        }
        else {
            throw "Unity project initialization failed. See log: $createLog"
        }
    }
}

function Ensure-ManifestDependencies {
    param(
        [string]$TargetProjectPath,
        [bool]$UseGitMirror
    )

    $packagesDir = Join-Path $TargetProjectPath "Packages"
    if (-not (Test-Path $packagesDir)) {
        New-Item -ItemType Directory -Path $packagesDir -Force | Out-Null
    }

    $manifestPath = Join-Path $packagesDir "manifest.json"
    if (Test-Path $manifestPath) {
        $rawManifest = Get-Content -Path $manifestPath -Raw
        $rawManifest = $rawManifest.TrimStart([char]0xFEFF)

        try {
            $manifestObject = $rawManifest | ConvertFrom-Json
        }
        catch {
            Write-Info "manifest.json parse failed, recreating a minimal manifest."
            $manifestObject = [pscustomobject]@{
                dependencies = [pscustomobject]@{}
            }
        }
    }
    else {
        $manifestObject = [pscustomobject]@{
            dependencies = [pscustomobject]@{}
        }
    }

    $deps = @{}
    if ($manifestObject.PSObject.Properties.Name -contains "dependencies" -and $manifestObject.dependencies) {
        foreach ($dep in $manifestObject.dependencies.PSObject.Properties) {
            $deps[$dep.Name] = $dep.Value
        }
    }

    if (-not $deps.Contains("com.unity.ugui")) {
        $deps["com.unity.ugui"] = "2.0.0"
    }

    if (-not $deps.Contains("com.unity.modules.ai")) {
        $deps["com.unity.modules.ai"] = "1.0.0"
    }

    if (-not $deps.Contains("com.unity.modules.ui")) {
        $deps["com.unity.modules.ui"] = "1.0.0"
    }

    if ($UseGitMirror) {
        $deps["com.mirror-networking.mirror"] = "https://github.com/MirrorNetworking/Mirror.git"
    }
    else {
        if ($deps.Contains("com.mirror-networking.mirror")) {
            $deps.Remove("com.mirror-networking.mirror")
        }
    }

    $manifestObject | Add-Member -MemberType NoteProperty -Name dependencies -Value ([pscustomobject]$deps) -Force
    $manifestJson = $manifestObject | ConvertTo-Json -Depth 50
    Write-TextNoBom -Path $manifestPath -Content $manifestJson
}

function Ensure-KenneyAssetPack {
    param(
        [string]$TargetProjectPath,
        [string]$AssetSlug,
        [string]$PackFolderName,
        [string]$MarkerRelativePath
    )

    $destinationRoot = Join-Path $TargetProjectPath ("Assets\ThirdParty\Kenney\" + $PackFolderName)
    $markerPath = Join-Path $destinationRoot $MarkerRelativePath
    if (Test-Path $markerPath) {
        Write-Info "Kenney pack '$PackFolderName' already present."
        return
    }

    $localFallbackMap = @{
        "furniture-kit" = "automation\\asset_probe_test\\extract"
        "animated-characters-1" = "automation\\asset_probe_test\\animated_chars"
        "impact-sounds" = "automation\\asset_probe_test\\impact"
        "rpg-audio" = "automation\\asset_probe_test\\rpg_audio"
        "sci-fi-sounds" = "automation\\asset_probe_test\\scifi_audio"
    }

    if ($localFallbackMap.ContainsKey($PackFolderName)) {
        $localSource = Join-Path $TargetProjectPath $localFallbackMap[$PackFolderName]
        if (Test-Path $localSource) {
            $markerLeaf = Split-Path -Path $MarkerRelativePath -Leaf
            $fallbackMarker = Get-ChildItem -Path $localSource -Recurse -File -Filter $markerLeaf -ErrorAction SilentlyContinue | Select-Object -First 1
            if ($null -ne $fallbackMarker) {
                Write-Info "Installing Kenney pack '$PackFolderName' from local cache..."
                if (Test-Path $destinationRoot) {
                    Remove-Item -LiteralPath $destinationRoot -Recurse -Force -ErrorAction SilentlyContinue
                }
                New-Item -ItemType Directory -Path $destinationRoot -Force | Out-Null
                Copy-Item -Path (Join-Path $localSource "*") -Destination $destinationRoot -Recurse -Force
                Write-Info "Kenney pack '$PackFolderName' installed from local cache."
                return
            }
        }
    }

    $assetPageUrl = "https://kenney.nl/assets/$AssetSlug"
    Write-Info "Downloading Kenney pack '$AssetSlug'..."
    $response = Invoke-WebRequest -Uri $assetPageUrl -UseBasicParsing
    $zipUrl = ($response.Content -split '"') |
        Where-Object { $_ -like "https://kenney.nl/media/pages/assets/*kenney_*zip" } |
        Select-Object -First 1

    if ([string]::IsNullOrWhiteSpace($zipUrl)) {
        throw "Could not find zip URL on page: $assetPageUrl"
    }

    $tmpRoot = Join-Path $TargetProjectPath ("automation\_kenney_tmp\" + $PackFolderName)
    $zipPath = Join-Path $tmpRoot ($PackFolderName + ".zip")
    $extractPath = Join-Path $tmpRoot "extract"

    if (Test-Path $tmpRoot) {
        Remove-Item -LiteralPath $tmpRoot -Recurse -Force -ErrorAction SilentlyContinue
    }

    New-Item -ItemType Directory -Path $tmpRoot -Force | Out-Null

    $oldProgress = $ProgressPreference
    $ProgressPreference = "SilentlyContinue"
    try {
        Invoke-WebRequest -Uri $zipUrl -OutFile $zipPath -UseBasicParsing
    }
    finally {
        $ProgressPreference = $oldProgress
    }

    Expand-Archive -Path $zipPath -DestinationPath $extractPath -Force

    $sourceRoot = $extractPath
    $directChildDirs = Get-ChildItem -Path $extractPath -Directory -ErrorAction SilentlyContinue
    $directChildFiles = Get-ChildItem -Path $extractPath -File -ErrorAction SilentlyContinue
    if ($directChildDirs.Count -eq 1 -and $directChildFiles.Count -eq 0) {
        $sourceRoot = $directChildDirs[0].FullName
    }

    if (Test-Path $destinationRoot) {
        Remove-Item -LiteralPath $destinationRoot -Recurse -Force -ErrorAction SilentlyContinue
    }
    New-Item -ItemType Directory -Path $destinationRoot -Force | Out-Null
    Copy-Item -Path (Join-Path $sourceRoot "*") -Destination $destinationRoot -Recurse -Force

    if (-not (Test-Path $markerPath)) {
        $markerLeaf = Split-Path -Path $MarkerRelativePath -Leaf
        $foundMarker = Get-ChildItem -Path $destinationRoot -Recurse -File -Filter $markerLeaf -ErrorAction SilentlyContinue | Select-Object -First 1
        if ($null -eq $foundMarker) {
            throw "Kenney pack '$PackFolderName' was copied but marker file not found: $markerPath"
        }
    }

    Remove-Item -LiteralPath $tmpRoot -Recurse -Force -ErrorAction SilentlyContinue
    Write-Info "Kenney pack '$PackFolderName' installed."
}

function Ensure-FreeAssetPacks {
    param([string]$TargetProjectPath)

    $packs = @(
        @{ slug = "furniture-kit"; folder = "furniture-kit"; marker = "Models\FBX format\wall.fbx" },
        @{ slug = "animated-characters-1"; folder = "animated-characters-1"; marker = "Model\characterMedium.fbx" },
        @{ slug = "impact-sounds"; folder = "impact-sounds"; marker = "footstep_concrete_000.ogg" },
        @{ slug = "rpg-audio"; folder = "rpg-audio"; marker = "doorOpen_1.ogg" },
        @{ slug = "sci-fi-sounds"; folder = "sci-fi-sounds"; marker = "doorOpen_000.ogg" }
    )

    foreach ($pack in $packs) {
        try {
            Ensure-KenneyAssetPack `
                -TargetProjectPath $TargetProjectPath `
                -AssetSlug $pack.slug `
                -PackFolderName $pack.folder `
                -MarkerRelativePath $pack.marker
        }
        catch {
            Write-Info "WARNING: failed to install '$($pack.folder)': $($_.Exception.Message)"
        }
    }
}

function Write-LaunchBatFiles {
    param([string]$ResolvedExePath)

    if ([string]::IsNullOrWhiteSpace($ResolvedExePath) -or -not (Test-Path $ResolvedExePath)) {
        return
    }

    $exeDirectory = Split-Path -Path $ResolvedExePath -Parent
    if ([string]::IsNullOrWhiteSpace($exeDirectory) -or -not (Test-Path $exeDirectory)) {
        return
    }

    $hostBatPath = Join-Path $exeDirectory "run_host.bat"
    $joinBatPath = Join-Path $exeDirectory "run_join.bat"

    $hostBat = @"
@echo off
setlocal
cd /d "%~dp0"
start "" "AsylumHorrorPrototype.exe" --host --port=7777
exit /b 0
"@

    $joinBat = @"
@echo off
setlocal
cd /d "%~dp0"
set /p TARGET_IP=Enter host public IP or direct host address: 
if "%TARGET_IP%"=="" (
  echo Host address is empty.
  pause
  exit /b 1
)
start "" "AsylumHorrorPrototype.exe" --join=%TARGET_IP% --port=7777
exit /b 0
"@

    Write-TextNoBom -Path $hostBatPath -Content $hostBat
    Write-TextNoBom -Path $joinBatPath -Content $joinBat
    Write-Info "Launcher bat files created:"
    Write-Host "  $hostBatPath"
    Write-Host "  $joinBatPath"
}

if ([string]::IsNullOrWhiteSpace($ProjectPath)) {
    $ProjectPath = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
}
else {
    $ProjectPath = (Resolve-Path $ProjectPath).Path
}

$buildExeEnabled = ConvertTo-BoolSafe $BuildExe

$unityExe = Find-UnityEditor
Write-Info "Unity: $unityExe"
Write-Info "Project: $ProjectPath"
Write-Info "Build EXE: $buildExeEnabled"

$hasGit = Test-GitAvailable
Write-Info "Git detected: $hasGit"

Ensure-ManifestDependencies -TargetProjectPath $ProjectPath -UseGitMirror $hasGit
Ensure-UnityProject -UnityExe $unityExe -TargetProjectPath $ProjectPath

if (-not $hasGit) {
    Ensure-MirrorSourceWithoutGit -TargetProjectPath $ProjectPath
}

Write-Info "Skipping legacy Kenney pack sync. Prototype uses in-project generated horror assets and procedural audio."

Stop-UnityInstancesForProject -TargetProjectPath $ProjectPath

$syncLog = Join-Path $ProjectPath "automation\unity_sync_packages.log"
Write-Info "Restoring packages and compiling..."
Invoke-UnityBatch -UnityExe $unityExe -UnityArgs @(
    "-batchmode",
    "-nographics",
    "-quit",
    "-projectPath", $ProjectPath
) -LogPath $syncLog -ProjectPathForLockHandling $ProjectPath

$defaultOutputExe = Join-Path $ProjectPath "Builds\Windows\AsylumHorrorPrototype.exe"
$resolvedOutputExe = if ([string]::IsNullOrWhiteSpace($OutputExePath)) { $defaultOutputExe } else { $OutputExePath }

$method = "AsylumHorror.EditorTools.HorrorPrototypeBuilder.CreateFullPrototype"
$buildLog = Join-Path $ProjectPath "automation\unity_create_prototype.log"

if ($buildExeEnabled) {
    $method = "AsylumHorror.EditorTools.HorrorPrototypeBuilder.CreatePrototypeAndBuildWindowsExe"
    $buildLog = Join-Path $ProjectPath "automation\unity_create_prototype_and_build.log"
    $env:HORROR_BUILD_EXE_PATH = $resolvedOutputExe
}

Write-Info "Running automation method: $method"
Invoke-UnityBatch -UnityExe $unityExe -UnityArgs @(
    "-batchmode",
    "-nographics",
    "-quit",
    "-projectPath", $ProjectPath,
    "-executeMethod", $method
) -LogPath $buildLog -ProjectPathForLockHandling $ProjectPath

if ($buildExeEnabled) {
    Remove-Item Env:HORROR_BUILD_EXE_PATH -ErrorAction SilentlyContinue
}

if ($buildExeEnabled -and (Test-Path $resolvedOutputExe)) {
    Write-Info "EXE ready: $resolvedOutputExe"
    Write-LaunchBatFiles -ResolvedExePath $resolvedOutputExe
}
elseif ($buildExeEnabled) {
    throw "Build method finished but EXE was not found: $resolvedOutputExe"
}

Write-Info "Done."
Write-Info "Logs:"
Write-Host "  $syncLog"
Write-Host "  $buildLog"
