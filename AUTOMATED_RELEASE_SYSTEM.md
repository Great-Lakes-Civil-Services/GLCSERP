# 🚀 GLERP Automated Release & Update System

## Overview
This guide sets up a complete automated release pipeline for GLERP that handles building, testing, packaging, and deploying updates automatically.

## 📋 System Components

### 1. Version Management
- Semantic versioning (MAJOR.MINOR.PATCH)
- Automated version bumping
- Release notes generation

### 2. Build Automation
- GitHub Actions workflows
- Automated testing
- Multi-platform builds
- Code signing (optional)

### 3. Release Management
- Automated GitHub releases
- Asset uploads
- Release notes
- Download page updates

### 4. Update Distribution
- Delta updates
- Auto-updater integration
- Rollback capabilities

## 🔧 Setup Instructions

### Step 1: Version Management

Create `version.json` in your project root:
```json
{
  "version": "1.0.0",
  "buildNumber": 1,
  "releaseDate": "2024-12-19",
  "changelog": [
    {
      "version": "1.0.0",
      "date": "2024-12-19",
      "changes": [
        "Initial release of GLERP",
        "Complete WPF-based ERP system",
        "User authentication with MFA",
        "Job management system",
        "Multi-window support"
      ]
    }
  ]
}
```

### Step 2: GitHub Actions Workflow

Create `.github/workflows/release.yml`:
```yaml
name: GLERP Release Pipeline

on:
  push:
    tags:
      - 'v*'
  workflow_dispatch:
    inputs:
      version:
        description: 'Release version'
        required: true
        default: '1.0.1'

jobs:
  build-and-release:
    runs-on: windows-latest
    
    steps:
    - name: Checkout code
      uses: actions/checkout@v4
      
    - name: Setup .NET
      uses: actions/setup-dotnet@v4
      with:
        dotnet-version: '9.0.x'
        
    - name: Restore dependencies
      run: dotnet restore
      
    - name: Build application
      run: dotnet build --configuration Release --no-restore
      
    - name: Run tests
      run: dotnet test --no-build --verbosity normal
      
    - name: Publish standalone
      run: |
        dotnet publish --configuration Release --output ./Release --self-contained true --runtime win-x64 --no-build
        dotnet publish --configuration Release --output ./Release-x86 --self-contained true --runtime win-x86 --no-build
        
    - name: Create release package
      run: |
        $version = "${{ github.event.inputs.version || github.ref_name }}"
        $version = $version.Replace("v", "")
        
        # Create ZIP packages
        Compress-Archive -Path "./Release/*" -DestinationPath "./GLERP-v$version-Standalone.zip" -Force
        Compress-Archive -Path "./Release-x86/*" -DestinationPath "./GLERP-v$version-Standalone-x86.zip" -Force
        
        # Create installer (optional)
        # iscc setup.iss /DMyAppVersion=$version
        
    - name: Update version info
      run: |
        $version = "${{ github.event.inputs.version || github.ref_name }}"
        $version = $version.Replace("v", "")
        
        $versionInfo = @{
          version = $version
          buildNumber = [int](Get-Date -UFormat %s)
          releaseDate = Get-Date -Format "yyyy-MM-dd"
          buildCommit = "${{ github.sha }}"
          buildBranch = "${{ github.ref_name }}"
        }
        
        $versionInfo | ConvertTo-Json | Out-File "./Release/version.json" -Encoding UTF8
        
    - name: Create Release
      uses: softprops/action-gh-release@v1
      with:
        files: |
          GLERP-v${{ github.event.inputs.version || github.ref_name }}-Standalone.zip
          GLERP-v${{ github.event.inputs.version || github.ref_name }}-Standalone-x86.zip
        body: |
          ## GLERP v${{ github.event.inputs.version || github.ref_name }}
          
          ### What's New
          - Automated release pipeline
          - Improved build process
          - Enhanced error handling
          
          ### System Requirements
          - Windows 10/11 (64-bit)
          - 4 GB RAM minimum
          - 500 MB disk space
          
          ### Installation
          1. Download and extract the ZIP file
          2. Run CivilProcessERP.exe
          3. Enter database credentials
          
          ### Download
          - **Windows x64**: GLERP-v${{ github.event.inputs.version || github.ref_name }}-Standalone.zip
          - **Windows x86**: GLERP-v${{ github.event.inputs.version || github.ref_name }}-Standalone-x86.zip
          
          ### Support
          - Email: support@greatlakescivilservices.com
          - Documentation: [Link to docs]
          
      env:
        GITHUB_TOKEN: ${{ secrets.GITHUB_TOKEN }}
        
    - name: Update download page
      run: |
        $version = "${{ github.event.inputs.version || github.ref_name }}"
        $version = $version.Replace("v", "")
        
        # Update download page with new version
        $downloadPage = Get-Content "./GLERP_Download_Page.html" -Raw
        $downloadPage = $downloadPage -replace "v1\.0\.0", "v$version"
        $downloadPage = $downloadPage -replace "GLERP-v1\.0\.0-Standalone\.zip", "GLERP-v$version-Standalone.zip"
        $downloadPage | Out-File "./GLERP_Download_Page.html" -Encoding UTF8
        
    - name: Deploy to hosting
      run: |
        # Upload updated files to your hosting provider
        # This could be Netlify, Vercel, or your own server
        echo "Deploying updated download page..."
        
    - name: Notify team
      run: |
        # Send notifications to team about new release
        echo "Release v${{ github.event.inputs.version || github.ref_name }} completed!"
```

### Step 3: Auto-Updater Integration

Create `Services/AutoUpdater.cs`:
```csharp
using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text.Json;
using System.Diagnostics;

namespace CivilProcessERP.Services
{
    public class AutoUpdater
    {
        private readonly string _updateUrl = "https://api.github.com/repos/Great-Lakes-Civil-Services/GLCSERP/releases/latest";
        private readonly string _currentVersion = "1.0.0";
        
        public async Task<UpdateInfo?> CheckForUpdatesAsync()
        {
            try
            {
                using var client = new HttpClient();
                client.DefaultRequestHeaders.Add("User-Agent", "GLERP-AutoUpdater");
                
                var response = await client.GetStringAsync(_updateUrl);
                var release = JsonSerializer.Deserialize<GitHubRelease>(response);
                
                if (release?.tag_name != null && IsNewerVersion(release.tag_name))
                {
                    return new UpdateInfo
                    {
                        Version = release.tag_name.Replace("v", ""),
                        DownloadUrl = GetDownloadUrl(release),
                        ReleaseNotes = release.body,
                        ReleaseDate = release.published_at
                    };
                }
                
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Update check failed: {ex.Message}");
                return null;
            }
        }
        
        private bool IsNewerVersion(string newVersion)
        {
            var current = Version.Parse(_currentVersion);
            var latest = Version.Parse(newVersion.Replace("v", ""));
            return latest > current;
        }
        
        private string GetDownloadUrl(GitHubRelease release)
        {
            var asset = release.assets?.FirstOrDefault(a => 
                a.name.Contains("Standalone") && a.name.EndsWith(".zip"));
            return asset?.browser_download_url ?? "";
        }
        
        public async Task<bool> DownloadAndInstallUpdateAsync(UpdateInfo updateInfo)
        {
            try
            {
                var tempPath = Path.GetTempPath();
                var zipPath = Path.Combine(tempPath, $"GLERP-{updateInfo.Version}.zip");
                
                // Download update
                using var client = new HttpClient();
                var zipData = await client.GetByteArrayAsync(updateInfo.DownloadUrl);
                await File.WriteAllBytesAsync(zipPath, zipData);
                
                // Extract and install
                var extractPath = Path.Combine(tempPath, $"GLERP-{updateInfo.Version}");
                System.IO.Compression.ZipFile.ExtractToDirectory(zipPath, extractPath);
                
                // Launch updater
                var updaterPath = Path.Combine(extractPath, "CivilProcessERP.exe");
                Process.Start(new ProcessStartInfo(updaterPath)
                {
                    Arguments = $"/update \"{Process.GetCurrentProcess().MainModule?.FileName}\"",
                    UseShellExecute = true
                });
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Update installation failed: {ex.Message}");
                return false;
            }
        }
    }
    
    public class UpdateInfo
    {
        public string Version { get; set; } = "";
        public string DownloadUrl { get; set; } = "";
        public string ReleaseNotes { get; set; } = "";
        public DateTime? ReleaseDate { get; set; }
    }
    
    public class GitHubRelease
    {
        public string? tag_name { get; set; }
        public string? body { get; set; }
        public DateTime? published_at { get; set; }
        public List<GitHubAsset>? assets { get; set; }
    }
    
    public class GitHubAsset
    {
        public string? name { get; set; }
        public string? browser_download_url { get; set; }
        public long size { get; set; }
    }
}
```

### Step 4: Update Notification System

Create `ViewModels/UpdateViewModel.cs`:
```csharp
using System;
using System.Windows.Input;
using CivilProcessERP.Services;
using CivilProcessERP.Helpers;

namespace CivilProcessERP.ViewModels
{
    public class UpdateViewModel : BaseViewModel
    {
        private readonly AutoUpdater _autoUpdater;
        private UpdateInfo? _availableUpdate;
        private bool _isChecking;
        private bool _isUpdating;
        
        public UpdateInfo? AvailableUpdate
        {
            get => _availableUpdate;
            set => SetProperty(ref _availableUpdate, value);
        }
        
        public bool IsChecking
        {
            get => _isChecking;
            set => SetProperty(ref _isChecking, value);
        }
        
        public bool IsUpdating
        {
            get => _isUpdating;
            set => SetProperty(ref _isUpdating, value);
        }
        
        public ICommand CheckForUpdatesCommand { get; }
        public ICommand InstallUpdateCommand { get; }
        public ICommand RemindLaterCommand { get; }
        
        public UpdateViewModel()
        {
            _autoUpdater = new AutoUpdater();
            
            CheckForUpdatesCommand = new RelayCommand(async () => await CheckForUpdatesAsync());
            InstallUpdateCommand = new RelayCommand(async () => await InstallUpdateAsync());
            RemindLaterCommand = new RelayCommand(() => RemindLater());
        }
        
        private async Task CheckForUpdatesAsync()
        {
            IsChecking = true;
            
            try
            {
                AvailableUpdate = await _autoUpdater.CheckForUpdatesAsync();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Update check failed: {ex.Message}", 
                    "Update Error", System.Windows.MessageBoxButton.OK, 
                    System.Windows.MessageBoxImage.Warning);
            }
            finally
            {
                IsChecking = false;
            }
        }
        
        private async Task InstallUpdateAsync()
        {
            if (AvailableUpdate == null) return;
            
            var result = System.Windows.MessageBox.Show(
                $"Install GLERP v{AvailableUpdate.Version}?\n\n{AvailableUpdate.ReleaseNotes}",
                "Install Update",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Question);
                
            if (result == System.Windows.MessageBoxResult.Yes)
            {
                IsUpdating = true;
                
                try
                {
                    var success = await _autoUpdater.DownloadAndInstallUpdateAsync(AvailableUpdate);
                    if (success)
                    {
                        System.Windows.Application.Current.Shutdown();
                    }
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"Update installation failed: {ex.Message}",
                        "Update Error", System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Error);
                }
                finally
                {
                    IsUpdating = false;
                }
            }
        }
        
        private void RemindLater()
        {
            AvailableUpdate = null;
        }
    }
}
```

### Step 5: Release Management Script

Create `scripts/manage-release.ps1`:
```powershell
param(
    [Parameter(Mandatory=$true)]
    [ValidateSet("patch", "minor", "major")]
    [string]$BumpType,
    
    [string]$Message = ""
)

Write-Host "🚀 GLERP Release Manager" -ForegroundColor Green
Write-Host "=========================" -ForegroundColor Green

# Read current version
$versionFile = "version.json"
$versionData = Get-Content $versionFile | ConvertFrom-Json
$currentVersion = [Version]$versionData.version

# Calculate new version
$newVersion = switch ($BumpType) {
    "patch" { [Version]::new($currentVersion.Major, $currentVersion.Minor, $currentVersion.Build + 1) }
    "minor" { [Version]::new($currentVersion.Major, $currentVersion.Minor + 1, 0) }
    "major" { [Version]::new($currentVersion.Major + 1, 0, 0) }
}

Write-Host "Current version: $currentVersion" -ForegroundColor Yellow
Write-Host "New version: $newVersion" -ForegroundColor Green

# Update version.json
$versionData.version = $newVersion.ToString()
$versionData.buildNumber = $versionData.buildNumber + 1
$versionData.releaseDate = Get-Date -Format "yyyy-MM-dd"

# Add to changelog
$changelogEntry = @{
    version = $newVersion.ToString()
    date = $versionData.releaseDate
    changes = @()
}

if ($Message) {
    $changelogEntry.changes = @($Message)
} else {
    $changelogEntry.changes = @("Bug fixes and improvements")
}

$versionData.changelog = @($changelogEntry) + $versionData.changelog

# Save updated version
$versionData | ConvertTo-Json -Depth 10 | Out-File $versionFile -Encoding UTF8

# Create git tag
$tagName = "v$newVersion"
git add $versionFile
git commit -m "Bump version to $newVersion"
git tag $tagName

Write-Host "✅ Version updated to $newVersion" -ForegroundColor Green
Write-Host "📝 Commit and tag created: $tagName" -ForegroundColor Green
Write-Host "🚀 Push to trigger release: git push --tags" -ForegroundColor Yellow
```

### Step 6: Continuous Integration

Create `.github/workflows/ci.yml`:
```yaml
name: GLERP CI

on:
  push:
    branches: [ main, develop ]
  pull_request:
    branches: [ main ]

jobs:
  build:
    runs-on: windows-latest
    
    steps:
    - uses: actions/checkout@v4
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v4
      with:
        dotnet-version: '9.0.x'
        
    - name: Restore dependencies
      run: dotnet restore
      
    - name: Build
      run: dotnet build --no-restore --verbosity normal
      
    - name: Test
      run: dotnet test --no-build --verbosity normal
      
    - name: Build standalone
      run: dotnet publish --configuration Release --output ./Release --self-contained true --runtime win-x64 --no-build
      
    - name: Upload artifacts
      uses: actions/upload-artifact@v4
      with:
        name: glerp-standalone
        path: ./Release/
```

## 📊 Release Workflow

### 1. Development Process
```bash
# Make changes to code
git add .
git commit -m "Add new feature"

# Bump version and create release
./scripts/manage-release.ps1 -BumpType minor -Message "Add new job management features"

# Push to trigger release
git push --tags
```

### 2. Automated Release Pipeline
1. **Tag Creation** → Triggers GitHub Actions
2. **Build & Test** → Compiles and tests the application
3. **Package Creation** → Creates ZIP files for distribution
4. **GitHub Release** → Creates release with assets
5. **Download Page Update** → Updates web pages automatically
6. **Deployment** → Deploys to hosting providers

### 3. User Update Process
1. **Check for Updates** → App checks GitHub API
2. **Download Update** → Downloads new version
3. **Install Update** → Extracts and replaces files
4. **Restart Application** → Launches updated version

## 🔧 Configuration

### Environment Variables
```bash
# GitHub Actions Secrets
GITHUB_TOKEN=your_github_token
NETLIFY_TOKEN=your_netlify_token
VERCEL_TOKEN=your_vercel_token

# Application Settings
UPDATE_CHECK_URL=https://api.github.com/repos/Great-Lakes-Civil-Services/GLCSERP/releases/latest
DOWNLOAD_BASE_URL=https://github.com/Great-Lakes-Civil-Services/GLCSERP/releases/download
```

### Release Checklist
- [ ] Update version.json
- [ ] Update changelog
- [ ] Test build locally
- [ ] Create git tag
- [ ] Push to trigger release
- [ ] Verify GitHub release
- [ ] Test download links
- [ ] Verify auto-updater
- [ ] Notify team

## 🚀 Quick Start

1. **Setup GitHub Actions**:
   ```bash
   # Copy workflow files
   cp .github/workflows/* .github/workflows/
   
   # Commit and push
   git add .
   git commit -m "Add automated release system"
   git push
   ```

2. **Create First Release**:
   ```bash
   # Bump version
   ./scripts/manage-release.ps1 -BumpType patch -Message "Initial automated release"
   
   # Push to trigger release
   git push --tags
   ```

3. **Test Update System**:
   - Deploy the application
   - Test auto-updater functionality
   - Verify download links work

## 📈 Monitoring & Analytics

### Release Metrics
- Download counts per version
- Update adoption rates
- Error rates and crash reports
- User feedback and ratings

### Health Checks
- Build success rates
- Test coverage
- Performance benchmarks
- Security scans

This automated system will streamline your release process and ensure consistent, professional updates for GLERP users! 