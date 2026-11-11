 # Configuration
$destDir = "C:\Users\29373\Pictures\Saved Pictures"
$destFile = "$destDir\wallpaper_Terminal.jpg"
$url = "https://picsum.photos/1920/1080"
$logFile = "$PSScriptRoot\..\bin\wallpaper_log.txt"

# Get current time for logging
$date = Get-Date -Format "yyyy-MM-dd HH:mm:ss"

try {
    # Ensure directory exists
    if (-not (Test-Path $destDir)) {
        New-Item -ItemType Directory -Path $destDir -Force | Out-Null
    }

    Write-Host "[$date] Downloading wallpaper from $url ..."
    
    # Download image
    Invoke-WebRequest -Uri $url -OutFile $destFile -UseBasicParsing
    
    $msg = "[$date] Success: Wallpaper updated to $destFile"
    Write-Host $msg
    Add-Content -Path $logFile -Value $msg

} catch {
    $errorMsg = "[$date] Error: Failed to download wallpaper. Details: $_"
    Write-Error $errorMsg
    Add-Content -Path $logFile -Value $errorMsg
}
