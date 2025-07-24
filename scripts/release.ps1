param(
    [string]$BumpType = "patch",
    [string]$Message = "Bug fixes and improvements"
)

Write-Host "GLERP Release Manager" -ForegroundColor Green
Write-Host "====================" -ForegroundColor Green

# Read current version
$versionFile = "version.json"
if (-not (Test-Path $versionFile)) {
    Write-Host "ERROR: version.json not found!" -ForegroundColor Red
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
    changes = @($Message)
}

$versionData.changelog = @($changelogEntry) + $versionData.changelog

# Save updated version
$versionData | ConvertTo-Json -Depth 10 | Out-File $versionFile -Encoding UTF8

# Create git tag
$tagName = "v$newVersion"

# Check if we're in a git repository
if (-not (Test-Path ".git")) {
    Write-Host "ERROR: Not in a git repository!" -ForegroundColor Red
    exit 1
}

# Add and commit changes
git add $versionFile
git commit -m "Bump version to $newVersion"

# Create tag
git tag $tagName

Write-Host "SUCCESS: Version updated to $newVersion" -ForegroundColor Green
Write-Host "Commit and tag created: $tagName" -ForegroundColor Green
Write-Host "Push to trigger release: git push --tags" -ForegroundColor Yellow 