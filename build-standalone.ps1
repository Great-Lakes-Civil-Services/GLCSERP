# GLERP Standalone Executable Build Script
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Building GLERP Standalone Executable" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

Write-Host ""
Write-Host "Cleaning previous builds..." -ForegroundColor Yellow
if (Test-Path "bin\Release") { Remove-Item "bin\Release" -Recurse -Force }
if (Test-Path "obj\Release") { Remove-Item "obj\Release" -Recurse -Force }

Write-Host ""
Write-Host "Building Standalone Release version..." -ForegroundColor Yellow
dotnet publish --configuration Release --runtime win-x64 --self-contained true --output "bin\Release\standalone"

if ($LASTEXITCODE -ne 0) {
    Write-Host ""
    Write-Host "❌ Build failed! Check the errors above." -ForegroundColor Red
    Read-Host "Press Enter to continue"
    exit 1
}

Write-Host ""
Write-Host "✅ Standalone build successful!" -ForegroundColor Green
Write-Host ""
Write-Host "📁 Standalone executable location: bin\Release\standalone\CivilProcessERP.exe" -ForegroundColor Cyan
Write-Host ""

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Creating Standalone Release Package" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

Write-Host ""
Write-Host "Creating standalone release folder..." -ForegroundColor Yellow
if (Test-Path "GLERP-Standalone") { Remove-Item "GLERP-Standalone" -Recurse -Force }
New-Item -ItemType Directory -Name "GLERP-Standalone" | Out-Null

Write-Host ""
Write-Host "Copying standalone executable and dependencies..." -ForegroundColor Yellow
Copy-Item "bin\Release\standalone\*" "GLERP-Standalone\" -Recurse -Force

Write-Host ""
Write-Host "Creating README file..." -ForegroundColor Yellow
$readmeContent = @"
GLERP v1.0.0 - Great Lakes Civil Services ERP System

========================================
STANDALONE EXECUTABLE
========================================

This is a standalone version of GLERP that includes
the .NET Runtime. No additional installation required!

Installation Instructions:
1. Extract all files to a folder
2. Double-click CivilProcessERP.exe to run
3. No .NET Runtime installation required!

System Requirements:
- Windows 10/11 (64-bit)
- 4 GB RAM minimum (8 GB recommended)
- 500 MB available disk space
- Internet connection for database connectivity

Build Date: $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")
Version: 1.0.0
Type: Standalone (Self-Contained)
"@

$readmeContent | Out-File "GLERP-Standalone\README.txt" -Encoding UTF8

Write-Host ""
Write-Host "✅ Standalone release package created in GLERP-Standalone folder!" -ForegroundColor Green
Write-Host ""
Write-Host "📦 Files ready for distribution:" -ForegroundColor Cyan
Write-Host "- CivilProcessERP.exe (Standalone executable)" -ForegroundColor White
Write-Host "- All .NET Runtime included" -ForegroundColor White
Write-Host "- No additional installation required" -ForegroundColor White
Write-Host "- README.txt (Installation instructions)" -ForegroundColor White
Write-Host ""

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Standalone Build Complete!" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Read-Host "Press Enter to continue" 