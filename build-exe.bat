@echo off
echo ========================================
echo Building GLERP Executable
echo ========================================

echo.
echo Cleaning previous builds...
if exist "bin\Release" rmdir /s /q "bin\Release"
if exist "obj\Release" rmdir /s /q "obj\Release"

echo.
echo Building Release version...
dotnet build --configuration Release --verbosity normal

if %ERRORLEVEL% NEQ 0 (
    echo.
    echo ❌ Build failed! Check the errors above.
    pause
    exit /b 1
)

echo.
echo ✅ Build successful!
echo.
echo 📁 Executable location: bin\Release\net9.0-windows\CivilProcessERP.exe
echo.

echo ========================================
echo Creating Release Package
echo ========================================

echo.
echo Creating release folder...
if exist "GLERP-Release" rmdir /s /q "GLERP-Release"
mkdir "GLERP-Release"

echo.
echo Copying executable and dependencies...
xcopy "bin\Release\net9.0-windows\*.*" "GLERP-Release\" /E /I /Y

echo.
echo Creating README file...
echo GLERP v1.0.0 - Great Lakes Civil Services ERP System > "GLERP-Release\README.txt"
echo. >> "GLERP-Release\README.txt"
echo Installation Instructions: >> "GLERP-Release\README.txt"
echo 1. Extract all files to a folder >> "GLERP-Release\README.txt"
echo 2. Double-click CivilProcessERP.exe to run >> "GLERP-Release\README.txt"
echo 3. Make sure you have .NET 9.0 Runtime installed >> "GLERP-Release\README.txt"
echo. >> "GLERP-Release\README.txt"
echo System Requirements: >> "GLERP-Release\README.txt"
echo - Windows 10/11 >> "GLERP-Release\README.txt"
echo - .NET 9.0 Runtime >> "GLERP-Release\README.txt"
echo - PostgreSQL database connection >> "GLERP-Release\README.txt"

echo.
echo ✅ Release package created in GLERP-Release folder!
echo.
echo 📦 Files ready for distribution:
echo - CivilProcessERP.exe (Main executable)
echo - All required DLLs and dependencies
echo - README.txt (Installation instructions)
echo.

echo ========================================
echo Build Complete!
echo ========================================
pause 