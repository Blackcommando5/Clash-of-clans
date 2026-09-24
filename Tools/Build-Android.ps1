param(
    [string]$UnityEditor = 'C:\Program Files\Unity\Hub\Editor\6000.3.13f1\Editor\Unity.exe'
)
$ErrorActionPreference = 'Stop'
$projectRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
if (!(Test-Path -LiteralPath $UnityEditor -PathType Leaf)) { throw "Unity editor not found: $UnityEditor" }
# Fresh copies avoid stale assets without deleting a user's files or interrupting their editor.
$buildCopy = Join-Path $projectRoot ('.utmp\AndroidBuild-' + [DateTime]::UtcNow.ToString('yyyyMMdd-HHmmss-fff'))
$outputRoot = Join-Path $projectRoot 'Builds\Android'
New-Item -ItemType Directory -Force -Path $buildCopy, $outputRoot | Out-Null
foreach ($folder in @('Assets', 'Packages', 'ProjectSettings')) {
    & robocopy (Join-Path $projectRoot $folder) (Join-Path $buildCopy $folder) /E /NFL /NDL /NJH /NJS /NP
    if ($LASTEXITCODE -ge 8) { throw "Copy failed: $folder (robocopy $LASTEXITCODE)" }
}
$revision = (& git -C $projectRoot rev-parse --short HEAD).Trim()
if ($LASTEXITCODE -ne 0) { throw 'Cannot identify source revision.' }
if (& git -C $projectRoot status --porcelain) { $revision += ' + working tree changes' }
$arguments = @('-batchmode', '-buildTarget', 'Android', '-projectPath', $buildCopy,
    '-executeMethod', 'AndroidDevelopmentBuild.Run', '-kingdomsBuildRoot', $outputRoot,
    '-kingdomsSourceRevision', $revision, '-logFile', (Join-Path $outputRoot 'unity-build.log'))
# Start-Process joins ArgumentList; quote each argument explicitly for Windows paths with spaces.
$quotedArguments = $arguments | ForEach-Object { '"' + $_.Replace('"', '\"') + '"' }
Write-Host "Building from $buildCopy. Log: $outputRoot\unity-build.log"
$buildStarted = [DateTime]::UtcNow
$process = Start-Process -FilePath $UnityEditor -ArgumentList ($quotedArguments -join ' ') -WindowStyle Hidden -PassThru -Wait
if ($process.ExitCode -ne 0) { throw "Unity failed with exit code $($process.ExitCode). Inspect the build log." }
$report = Join-Path $outputRoot 'build-report.txt'
if (!(Test-Path -LiteralPath $report) -or !(Select-String -LiteralPath $report -Pattern '^Result: Succeeded$' -Quiet)) { throw 'Missing successful build report.' }
if ((Get-Item -LiteralPath $report).LastWriteTimeUtc -lt $buildStarted) { throw 'Build report is stale.' }
Get-Content -LiteralPath $report
Get-FileHash -LiteralPath (Join-Path $outputRoot 'Kingdoms-development.apk') -Algorithm SHA256
