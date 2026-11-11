# Configuration
$cscPath = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"

$srcRoot = Join-Path $PSScriptRoot "src"
$iconFile = Join-Path $srcRoot "Assets\app.ico"
$manifestFile = Join-Path $srcRoot "Assets\app.manifest"
$themeFile = Join-Path $srcRoot "Generated\Theme.Generated.cs"
$stringsFile = Join-Path $srcRoot "Generated\Strings.Generated.cs"

if (-not (Test-Path $cscPath)) {
    Write-Host "Error: csc.exe not found at $cscPath" -ForegroundColor Red
    exit 1
}

# 生成物缺失时直接报错，不静默回退（见 design/README.md）
foreach ($required in @($iconFile, $manifestFile, $themeFile, $stringsFile)) {
    if (-not (Test-Path $required)) {
        Write-Host "Error: 缺少必要文件 $required" -ForegroundColor Red
        Write-Host "生成物请先运行 python3 tools/gen_tokens.py" -ForegroundColor Yellow
        exit 1
    }
}

$sources = @(Get-ChildItem -Path $srcRoot -Recurse -Filter *.cs |
    Sort-Object FullName |
    ForEach-Object { $_.FullName })

if ($sources.Count -eq 0) {
    Write-Host "Error: src 目录下没有找到任何 .cs 文件" -ForegroundColor Red
    exit 1
}

Write-Host "Found csc.exe at $cscPath" -ForegroundColor Green
Write-Host ("Compiling {0} source files..." -f $sources.Count) -ForegroundColor Cyan

# Ensure bin directory exists
$binDir = Join-Path $PSScriptRoot "bin"
if (-not (Test-Path $binDir)) {
    New-Item -ItemType Directory -Path $binDir | Out-Null
}

$outFile = Join-Path $binDir "WallpaperTool.exe"

# Compile command：源码为 UTF-8，显式指定代码页避免中文乱码
$compilerArgs = @(
    "/nologo",
    "/target:winexe",
    "/optimize+",
    "/codepage:65001",
    "/out:$outFile",
    "/win32icon:$iconFile",
    "/win32manifest:$manifestFile"
) + $sources

$process = Start-Process -FilePath $cscPath -ArgumentList $compilerArgs -Wait -NoNewWindow -PassThru

if ($process.ExitCode -eq 0) {
    Write-Host "`nBuild SUCCESS!" -ForegroundColor Green
    Write-Host "Created: $outFile`n"
} else {
    Write-Host "`nBuild FAILED!" -ForegroundColor Red
    exit $process.ExitCode
}
