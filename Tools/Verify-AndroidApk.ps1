param(
    [string]$Apk = (Join-Path $PSScriptRoot '..\Builds\Android\Kingdoms-development.apk'),
    [string]$AndroidPlayer = 'C:\Program Files\Unity\Hub\Editor\6000.3.13f1\Editor\Data\PlaybackEngines\AndroidPlayer'
)
$ErrorActionPreference = 'Stop'
$apkPath = (Resolve-Path -LiteralPath $Apk).Path
$buildTools = Join-Path $AndroidPlayer 'SDK\build-tools\36.0.0'
$badging = & (Join-Path $buildTools 'aapt.exe') dump badging $apkPath
if ($LASTEXITCODE -ne 0) { throw 'Cannot read APK manifest.' }
if (!($badging -match "package: name='com.kingdoms.prototype'")) { throw 'Unexpected package ID.' }
if (!($badging -match "native-code: 'arm64-v8a'")) { throw 'Expected ARM64 native libraries.' }
$signature = & (Join-Path $AndroidPlayer 'OpenJDK\bin\java.exe') -jar (Join-Path $buildTools 'lib\apksigner.jar') verify --verbose --print-certs $apkPath
if ($LASTEXITCODE -ne 0) { throw 'APK signature verification failed.' }
& (Join-Path $buildTools 'zipalign.exe') -c -P 16 4 $apkPath
if ($LASTEXITCODE -ne 0) { throw 'APK alignment verification failed.' }
Add-Type -AssemblyName System.IO.Compression.FileSystem
$archive = [IO.Compression.ZipFile]::OpenRead($apkPath)
try {
    $names = @($archive.Entries | ForEach-Object FullName)
    foreach ($required in @('lib/arm64-v8a/libunity.so', 'lib/arm64-v8a/libil2cpp.so')) {
        if ($names -notcontains $required) { throw "Missing native library: $required" }
    }
    if ($names -match '^lib/(armeabi|x86)') { throw 'Unexpected non-ARM64 library.' }
} finally { $archive.Dispose() }
$report = @('PASS: package, ARM64 Unity/IL2CPP libraries, APK signature and 16-KiB ZIP alignment.',
    'This is package validation; installation, launch, touch, resume and performance need a device.',
    ($badging -join "`n"), ($signature -join "`n"),
    ('SHA256: ' + (Get-FileHash -LiteralPath $apkPath -Algorithm SHA256).Hash)) -join "`n"
$report | Set-Content -LiteralPath (Join-Path (Split-Path $apkPath) 'apk-verification.txt')
Write-Output $report
