@echo off
chcp 65001 >nul
echo 正在更新壁纸...
powershell -ExecutionPolicy Bypass -File "%~dp0download_wallpaper.ps1"
if %ERRORLEVEL% EQU 0 (
    echo 成功！窗口将在 3 秒后关闭。
    timeout /t 3 >nul
) else (
    echo 失败！按任意键退出。
    pause
)
