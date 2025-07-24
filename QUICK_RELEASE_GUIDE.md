# 🚀 GLERP Quick Release Guide

## Overview
This guide shows you how to use the automated release system for GLERP. The system handles building, testing, packaging, and deploying updates automatically.

## 📋 Prerequisites

1. **GitHub Repository**: Your code must be in a GitHub repository
2. **GitHub Actions**: Enabled in your repository settings
3. **Git Tags**: Permission to create and push tags

## 🚀 Quick Start

### Step 1: Setup (One-time)

1. **Verify Files Exist**:
   ```bash
   # Check that these files exist
   ls version.json
   ls .github/workflows/release.yml
   ls .github/workflows/ci.yml
   ls scripts/manage-release.ps1
   ```

2. **Push to GitHub**:
   ```bash
   git add .
   git commit -m "Add automated release system"
   git push origin main
   ```

### Step 2: Create Your First Release

1. **Make Changes**: Edit your code as needed
2. **Commit Changes**:
   ```bash
   git add .
   git commit -m "Add new feature"
   ```

3. **Create Release**:
   ```bash
   # For bug fixes (1.0.0 → 1.0.1)
   ./scripts/manage-release.ps1 -BumpType patch -Message "Fix login issue"
   
   # For new features (1.0.0 → 1.1.0)
   ./scripts/manage-release.ps1 -BumpType minor -Message "Add job search feature"
   
   # For major changes (1.0.0 → 2.0.0)
   ./scripts/manage-release.ps1 -BumpType major -Message "Complete UI redesign"
   ```

4. **Push to Trigger Release**:
   ```bash
   git push
   git push --tags
   ```

### Step 3: Monitor Release

1. **Check GitHub Actions**: Go to your repository → Actions tab
2. **Monitor Progress**: Watch the build and release process
3. **Verify Release**: Check the Releases tab for your new release
4. **Test Download**: Verify the download links work

## 📊 Release Types

### Patch Release (1.0.0 → 1.0.1)
- Bug fixes
- Minor improvements
- Security updates

```bash
./scripts/manage-release.ps1 -BumpType patch -Message "Fix database connection timeout"
```

### Minor Release (1.0.0 → 1.1.0)
- New features
- Enhancements
- Non-breaking changes

```bash
./scripts/manage-release.ps1 -BumpType minor -Message "Add advanced search functionality"
```

### Major Release (1.0.0 → 2.0.0)
- Breaking changes
- Major redesigns
- Significant new features

```bash
./scripts/manage-release.ps1 -BumpType major -Message "Complete UI redesign with new dashboard"
```

## 🔧 Configuration

### Environment Variables
Set these in your GitHub repository settings (Settings → Secrets):

```bash
# Required
GITHUB_TOKEN=your_github_token

# Optional (for deployment)
NETLIFY_TOKEN=your_netlify_token
VERCEL_TOKEN=your_vercel_token
```

### Customize Release Notes
Edit the release template in `.github/workflows/release.yml`:

```yaml
body: |
  ## GLERP v${{ github.event.inputs.version || github.ref_name }}
  
  ### What's New
  - Your custom release notes here
  - Add more bullet points as needed
  
  ### System Requirements
  - Windows 10/11 (64-bit)
  - 4 GB RAM minimum
  - 500 MB disk space
```

## 📈 Monitoring

### GitHub Actions Dashboard
- **URL**: `https://github.com/Great-Lakes-Civil-Services/GLCSERP/actions`
- **Monitor**: Build status, test results, deployment progress

### Release Analytics
- **Downloads**: Track per-version download counts
- **Adoption**: Monitor update adoption rates
- **Issues**: Track bugs and user feedback

### Health Checks
- **Build Success Rate**: Should be >95%
- **Test Coverage**: Aim for >80%
- **Release Frequency**: Regular releases (weekly/monthly)

## 🚨 Troubleshooting

### Common Issues

1. **Build Fails**:
   ```bash
   # Check local build
   dotnet build --configuration Release
   
   # Check for missing dependencies
   dotnet restore
   ```

2. **Release Not Created**:
   - Verify git tag was pushed: `git push --tags`
   - Check GitHub Actions permissions
   - Verify workflow file syntax

3. **Download Links Broken**:
   - Check GitHub release assets
   - Verify file names match expected format
   - Test download URLs manually

### Debug Commands

```bash
# Check current version
cat version.json

# Verify git status
git status
git log --oneline -5

# Test local build
dotnet build --configuration Release

# Check GitHub Actions
gh run list --limit 10
```

## 📋 Release Checklist

Before creating a release:

- [ ] **Code Review**: All changes reviewed and approved
- [ ] **Testing**: Local build and test successful
- [ ] **Documentation**: README and changelog updated
- [ ] **Version**: version.json updated correctly
- [ ] **Git Status**: All changes committed
- [ ] **Backup**: Important data backed up
- [ ] **Team Notification**: Team aware of release

After release:

- [ ] **GitHub Release**: Created successfully
- [ ] **Download Links**: Working correctly
- [ ] **Auto-updater**: Functioning properly
- [ ] **User Notification**: Users informed of update
- [ ] **Monitoring**: Watch for issues
- [ ] **Feedback**: Collect user feedback

## 🎯 Best Practices

### Release Frequency
- **Patch**: As needed (bug fixes)
- **Minor**: Monthly (new features)
- **Major**: Quarterly (major changes)

### Version Naming
- Use semantic versioning (MAJOR.MINOR.PATCH)
- Include meaningful release notes
- Tag releases with descriptive names

### Quality Assurance
- Test locally before releasing
- Use staging environment if possible
- Monitor release metrics
- Collect user feedback

### Communication
- Notify users of important updates
- Provide clear migration guides
- Document breaking changes
- Offer support for issues

## 📞 Support

If you encounter issues:

1. **Check Logs**: GitHub Actions logs for detailed error messages
2. **Verify Setup**: Ensure all files are in place
3. **Test Locally**: Run build commands locally
4. **Review Permissions**: Check GitHub repository settings
5. **Contact Team**: Reach out for assistance

## 🚀 Next Steps

1. **First Release**: Create your first automated release
2. **Monitor**: Watch the process and verify everything works
3. **Customize**: Adjust settings for your specific needs
4. **Scale**: Add more automation as needed
5. **Optimize**: Improve based on feedback and metrics

This automated system will save you hours of manual work and ensure consistent, professional releases for GLERP! 