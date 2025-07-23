# GLERP Release Build Script
# This script builds and packages GLERP for distribution

param(
    [string]$Version = "1.0.0",
    [string]$Configuration = "Release"
)

Write-Host "🚀 GLERP Release Build Script" -ForegroundColor Green
Write-Host "Version: $Version" -ForegroundColor Yellow
Write-Host "Configuration: $Configuration" -ForegroundColor Yellow
Write-Host ""

# Set error action preference
$ErrorActionPreference = "Stop"

# Create release directory
$ReleaseDir = "./Release"
$PackageDir = "./Package"

if (Test-Path $ReleaseDir) {
    Write-Host "Cleaning previous release..." -ForegroundColor Yellow
    Remove-Item $ReleaseDir -Recurse -Force
}

if (Test-Path $PackageDir) {
    Write-Host "Cleaning previous package..." -ForegroundColor Yellow
    Remove-Item $PackageDir -Recurse -Force
}

# Create directories
New-Item -ItemType Directory -Path $ReleaseDir -Force | Out-Null
New-Item -ItemType Directory -Path $PackageDir -Force | Out-Null

Write-Host "📦 Building GLERP..." -ForegroundColor Green

# Build the project
try {
    dotnet build --configuration $Configuration --no-restore
    if ($LASTEXITCODE -ne 0) {
        throw "Build failed with exit code $LASTEXITCODE"
    }
    Write-Host "✅ Build completed successfully" -ForegroundColor Green
}
catch {
    Write-Host "❌ Build failed: $_" -ForegroundColor Red
    exit 1
}

Write-Host "📤 Publishing GLERP..." -ForegroundColor Green

# Publish the application
try {
    dotnet publish --configuration $Configuration --output $ReleaseDir --self-contained true --runtime win-x64 --no-build
    if ($LASTEXITCODE -ne 0) {
        throw "Publish failed with exit code $LASTEXITCODE"
    }
    Write-Host "✅ Publish completed successfully" -ForegroundColor Green
}
catch {
    Write-Host "❌ Publish failed: $_" -ForegroundColor Red
    exit 1
}

# Create release package
Write-Host "📦 Creating release package..." -ForegroundColor Green

# Copy additional files
$AdditionalFiles = @(
    "README.md",
    "LICENSE",
    "CHANGELOG.md"
)

foreach ($file in $AdditionalFiles) {
    if (Test-Path $file) {
        Copy-Item $file $ReleaseDir
        Write-Host "✅ Copied $file" -ForegroundColor Green
    }
}

# Create version file
$VersionInfo = @{
    Version = $Version
    BuildDate = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    BuildMachine = $env:COMPUTERNAME
    BuildUser = $env:USERNAME
}

$VersionInfo | ConvertTo-Json | Out-File "$ReleaseDir/version.json" -Encoding UTF8

# Create ZIP package
$ZipName = "GLERP-v$Version.zip"
$ZipPath = "$PackageDir/$ZipName"

try {
    Compress-Archive -Path "$ReleaseDir/*" -DestinationPath $ZipPath -Force
    Write-Host "✅ Created ZIP package: $ZipName" -ForegroundColor Green
}
catch {
    Write-Host "❌ Failed to create ZIP package: $_" -ForegroundColor Red
    exit 1
}

# Get file size
$FileSize = (Get-Item $ZipPath).Length
$FileSizeMB = [math]::Round($FileSize / 1MB, 2)

Write-Host ""
Write-Host "🎉 GLERP Release Package Created Successfully!" -ForegroundColor Green
Write-Host "================================================" -ForegroundColor Green
Write-Host "📁 Release Directory: $ReleaseDir" -ForegroundColor Yellow
Write-Host "📦 Package File: $ZipPath" -ForegroundColor Yellow
Write-Host "📏 File Size: $FileSizeMB MB" -ForegroundColor Yellow
Write-Host "🏷️  Version: $Version" -ForegroundColor Yellow
Write-Host ""

# Create installation instructions
$InstallInstructions = @"
# GLERP Installation Instructions

## System Requirements
- Windows 10/11 (64-bit)
- 4 GB RAM minimum (8 GB recommended)
- 500 MB available disk space
- Internet connection for database connectivity

## Installation Steps

1. **Extract the ZIP file** to a temporary location
2. **Run CivilProcessERP.exe** from the extracted folder
3. **Enter database credentials** when prompted (provided by IT)
4. **Set up MFA** on first login (if enabled)
5. **Start using GLERP!**

## Support
- Email: support@greatlakescivilservices.com
- Phone: (555) 123-4567
- Hours: Monday-Friday, 8:00 AM - 5:00 PM

## Troubleshooting
- Ensure you have proper database access
- Check Windows Firewall settings
- Verify .NET Runtime is installed
- Contact IT for database connection issues

Version: $Version
Build Date: $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")
"@

$InstallInstructions | Out-File "$PackageDir/INSTALLATION_GUIDE.txt" -Encoding UTF8
Write-Host "📖 Created installation guide" -ForegroundColor Green

Write-Host ""
Write-Host "🚀 Ready for distribution!" -ForegroundColor Green
Write-Host "Share the ZIP file with your office staff." -ForegroundColor Yellow
Write-Host "" 