@echo off
echo ========================================
echo Building GLERP Standalone Executable
echo ========================================

echo.
echo Cleaning previous builds...
if exist "bin\Release" rmdir /s /q "bin\Release"
if exist "obj\Release" rmdir /s /q "obj\Release"

echo.
echo Building Standalone Release version...
dotnet publish --configuration Release --runtime win-x64 --self-contained true --output "bin\Release\standalone"

if %ERRORLEVEL% NEQ 0 (
    echo.
    echo ❌ Build failed! Check the errors above.
    pause
    exit /b 1
)

echo.
echo ✅ Standalone build successful!
echo.
echo 📁 Standalone executable location: bin\Release\standalone\CivilProcessERP.exe
echo.

echo ========================================
echo Creating Standalone Release Package
echo ========================================

echo.
echo Creating standalone release folder...
if exist "GLERP-Standalone" rmdir /s /q "GLERP-Standalone"
mkdir "GLERP-Standalone"

echo.
echo Copying standalone executable and dependencies...
xcopy "bin\Release\standalone\*.*" "GLERP-Standalone\" /E /I /Y

echo.
echo Creating README file...
echo GLERP v1.0.0 - Great Lakes Civil Services ERP System > "GLERP-Standalone\README.txt"
echo. >> "GLERP-Standalone\README.txt"
echo ======================================== >> "GLERP-Standalone\README.txt"
echo STANDALONE EXECUTABLE >> "GLERP-Standalone\README.txt"
echo ======================================== >> "GLERP-Standalone\README.txt"
echo. >> "GLERP-Standalone\README.txt"
echo This is a standalone version of GLERP that includes >> "GLERP-Standalone\README.txt"
echo the .NET Runtime. No additional installation required! >> "GLERP-Standalone\README.txt"
echo. >> "GLERP-Standalone\README.txt"
echo Installation Instructions: >> "GLERP-Standalone\README.txt"
echo 1. Extract all files to a folder >> "GLERP-Standalone\README.txt"
echo 2. Double-click CivilProcessERP.exe to run >> "GLERP-Standalone\README.txt"
echo 3. No .NET Runtime installation required! >> "GLERP-Standalone\README.txt"
echo. >> "GLERP-Standalone\README.txt"
echo System Requirements: >> "GLERP-Standalone\README.txt"
echo - Windows 10/11 (64-bit) >> "GLERP-Standalone\README.txt"
echo - 4 GB RAM minimum (8 GB recommended) >> "GLERP-Standalone\README.txt"
echo - 500 MB available disk space >> "GLERP-Standalone\README.txt"
echo - Internet connection for database connectivity >> "GLERP-Standalone\README.txt"
echo. >> "GLERP-Standalone\README.txt"
echo Build Date: %date% %time% >> "GLERP-Standalone\README.txt"
echo Version: 1.0.0 >> "GLERP-Standalone\README.txt"
echo Type: Standalone (Self-Contained) >> "GLERP-Standalone\README.txt"

echo.
echo ✅ Standalone release package created in GLERP-Standalone folder!
echo.
echo 📦 Files ready for distribution:
echo - CivilProcessERP.exe (Standalone executable)
echo - All .NET Runtime included
echo - No additional installation required
echo - README.txt (Installation instructions)
echo.

echo ========================================
echo Standalone Build Complete!
echo ========================================
pause 