@echo off
echo 🚀 GLERP Release Tool
echo ====================
echo.

if "%1"=="" (
    echo Usage: release.bat [patch^|minor^|major] [message]
    echo.
    echo Examples:
    echo   release.bat patch "Fix login bug"
    echo   release.bat minor "Add search feature"
    echo   release.bat major "Complete redesign"
    echo.
    pause
    exit /b 1
)

set BUMP_TYPE=%1
shift
set MESSAGE=%*

if "%MESSAGE%"=="" (
    set MESSAGE="Bug fixes and improvements"
)

echo Creating %BUMP_TYPE% release...
echo Message: %MESSAGE%
echo.

powershell -ExecutionPolicy Bypass -File "scripts/release.ps1" -BumpType %BUMP_TYPE% -Message "%MESSAGE%"

if %errorlevel% equ 0 (
    echo.
    echo ✅ Release created successfully!
    echo.
    echo Next steps:
    echo 1. git push
    echo 2. git push --tags
    echo 3. Monitor: https://github.com/Great-Lakes-Civil-Services/GLCSERP/actions
    echo.
) else (
    echo.
    echo ❌ Release creation failed!
    echo Check the error messages above.
    echo.
)

pause 