@echo off
echo 🚀 GLERP Release Builder
echo ========================
echo.

REM Check if PowerShell is available
powershell -Command "Get-Host" >nul 2>&1
if %errorlevel% neq 0 (
    echo ❌ PowerShell is not available on this system.
    echo Please install PowerShell and try again.
    pause
    exit /b 1
)

REM Run the PowerShell build script
echo 📦 Starting GLERP build process...
echo.

powershell -ExecutionPolicy Bypass -File "build-release.ps1" -Version "1.0.0"

if %errorlevel% equ 0 (
    echo.
    echo ✅ Build completed successfully!
    echo 📦 Check the Package folder for your release files.
) else (
    echo.
    echo ❌ Build failed! Check the error messages above.
)

echo.
pause 