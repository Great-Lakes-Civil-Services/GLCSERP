@echo off
echo GLERP Release Tool
echo ====================
echo.

if "%1"=="" (
    echo Usage: release-simple.bat [patch^|minor^|major]
    echo.
    echo Examples:
    echo   release-simple.bat patch
    echo   release-simple.bat minor
    echo   release-simple.bat major
    echo.
    pause
    exit /b 1
)

set BUMP_TYPE=%1

echo Creating %BUMP_TYPE% release...
echo.

powershell -ExecutionPolicy Bypass -File "scripts/release.ps1" -BumpType %BUMP_TYPE%

if %errorlevel% equ 0 (
    echo.
    echo SUCCESS: Release created successfully!
    echo.
    echo Next steps:
    echo 1. git push
    echo 2. git push --tags
    echo 3. Monitor: https://github.com/Great-Lakes-Civil-Services/GLCSERP/actions
    echo.
) else (
    echo.
    echo ERROR: Release creation failed!
    echo Check the error messages above.
    echo.
)

pause 