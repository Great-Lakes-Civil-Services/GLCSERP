# GLERP Executable Build Script
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Building GLERP Executable" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

Write-Host ""
Write-Host "Cleaning previous builds..." -ForegroundColor Yellow
if (Test-Path "bin\Release") { Remove-Item "bin\Release" -Recurse -Force }
if (Test-Path "obj\Release") { Remove-Item "obj\Release" -Recurse -Force }

Write-Host ""
Write-Host "Building Release version..." -ForegroundColor Yellow
dotnet build --configuration Release --verbosity normal

if ($LASTEXITCODE -ne 0) {
    Write-Host ""
    Write-Host "❌ Build failed! Check the errors above." -ForegroundColor Red
    Read-Host "Press Enter to continue"
    exit 1
}

Write-Host ""
Write-Host "✅ Build successful!" -ForegroundColor Green
Write-Host ""
Write-Host "📁 Executable location: bin\Release\net9.0-windows\CivilProcessERP.exe" -ForegroundColor Cyan
Write-Host ""

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Creating Release Package" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

Write-Host ""
Write-Host "Creating release folder..." -ForegroundColor Yellow
if (Test-Path "GLERP-Release") { Remove-Item "GLERP-Release" -Recurse -Force }
New-Item -ItemType Directory -Name "GLERP-Release" | Out-Null

Write-Host ""
Write-Host "Copying executable and dependencies..." -ForegroundColor Yellow
Copy-Item "bin\Release\net9.0-windows\*" "GLERP-Release\" -Recurse -Force

Write-Host ""
Write-Host "Creating README file..." -ForegroundColor Yellow
$readmeContent = @"
GLERP v1.0.0 - Great Lakes Civil Services ERP System

Installation Instructions:
1. Extract all files to a folder
2. Double-click CivilProcessERP.exe to run
3. Make sure you have .NET 9.0 Runtime installed

System Requirements:
- Windows 10/11
- .NET 9.0 Runtime
- PostgreSQL database connection

Release Date: $(Get-Date -Format "yyyy-MM-dd")
Version: 1.0.0
"@

$readmeContent | Out-File "GLERP-Release\README.txt" -Encoding UTF8

Write-Host ""
Write-Host "✅ Release package created in GLERP-Release folder!" -ForegroundColor Green
Write-Host ""
Write-Host "📦 Files ready for distribution:" -ForegroundColor Cyan
Write-Host "- CivilProcessERP.exe (Main executable)" -ForegroundColor White
Write-Host "- All required DLLs and dependencies" -ForegroundColor White
Write-Host "- README.txt (Installation instructions)" -ForegroundColor White
Write-Host ""

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Build Complete!" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Read-Host "Press Enter to continue" 