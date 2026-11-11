# Configuration
$cscPath = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"

if (-not (Test-Path $cscPath)) {
    Write-Host "Error: csc.exe not found at $cscPath" -ForegroundColor Red
    exit 1
}

Write-Host "Found csc.exe at $cscPath" -ForegroundColor Green
Write-Host "Compiling WallpaperApp.cs..." -ForegroundColor Cyan

# Ensure bin directory exists
$binDir = Join-Path $PSScriptRoot "bin"
if (-not (Test-Path $binDir)) {
    New-Item -ItemType Directory -Path $binDir | Out-Null
}

# Compile command
$srcFile = Join-Path $PSScriptRoot "src\WallpaperApp.cs"
$outFile = Join-Path $binDir "WallpaperTool.exe"
$iconFile = Join-Path $PSScriptRoot "src\app.ico"
$manifestFile = Join-Path $PSScriptRoot "src\app.manifest"

$args = @(
    "/target:winexe",
    "/out:$outFile",
    "/win32icon:$iconFile",
    "/win32manifest:$manifestFile",
    $srcFile
)

# Run csc.exe
$process = Start-Process -FilePath $cscPath -ArgumentList $args -Wait -NoNewWindow -PassThru

if ($process.ExitCode -eq 0) {
    Write-Host "`nBuild SUCCESS!" -ForegroundColor Green
    Write-Host "Created: $outFile`n"
} else {
    Write-Host "`nBuild FAILED!" -ForegroundColor Red
    exit $process.ExitCode
}
