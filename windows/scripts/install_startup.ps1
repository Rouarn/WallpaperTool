$WshShell = New-Object -comObject WScript.Shell
$StartupDir = $WshShell.SpecialFolders.Item("Startup")
$ShortcutPath = "$StartupDir\UpdateTerminalWallpaper.lnk"

# Target points to the new EXE file
# Assuming install_startup.ps1 is in scripts directory, EXE is in bin directory
$TargetExe = Join-Path $PSScriptRoot "..\bin\WallpaperTool.exe"
$TargetExe = [System.IO.Path]::GetFullPath($TargetExe)

try {
    if (-not (Test-Path $TargetExe)) {
        throw "WallpaperTool.exe not found at $TargetExe. Please build the project first."
    }

    $Shortcut = $WshShell.CreateShortcut($ShortcutPath)
    # Point directly to EXE file
    $Shortcut.TargetPath = $TargetExe
    # Add -silent argument to trigger background auto-download logic
    $Shortcut.Arguments = "-silent"
    $Shortcut.WorkingDirectory = [System.IO.Path]::GetDirectoryName($TargetExe)
    $Shortcut.Description = "Automatically update terminal wallpaper on login"
    $Shortcut.Save()

    Write-Host "Startup shortcut successfully created at:"
    Write-Host $ShortcutPath
    Write-Host "`nWallpaper tool will now run automatically (silently) when you login."
} catch {
    Write-Error "Failed to create startup shortcut: $_"
}
