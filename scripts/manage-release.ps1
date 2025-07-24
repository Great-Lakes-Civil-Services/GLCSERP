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
if (-not (Test-Path $versionFile)) {
    Write-Host "❌ version.json not found!" -ForegroundColor Red
    exit 1
}

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

# Check if we're in a git repository
if (-not (Test-Path ".git")) {
    Write-Host "❌ Not in a git repository!" -ForegroundColor Red
    exit 1
}

# Add and commit changes
git add $versionFile
git commit -m "Bump version to $newVersion"

# Create tag
git tag $tagName

Write-Host "✅ Version updated to $newVersion" -ForegroundColor Green
Write-Host "📝 Commit and tag created: $tagName" -ForegroundColor Green
Write-Host "🚀 Push to trigger release: git push --tags" -ForegroundColor Yellow
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Cyan
Write-Host "1. Review changes: git log --oneline -5" -ForegroundColor White
Write-Host "2. Push changes: git push" -ForegroundColor White
Write-Host "3. Push tags: git push --tags" -ForegroundColor White
Write-Host "4. Monitor release: https://github.com/Great-Lakes-Civil-Services/GLCSERP/releases" -ForegroundColor White 