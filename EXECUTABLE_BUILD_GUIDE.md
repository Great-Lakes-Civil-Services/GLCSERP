# 🚀 GLERP Executable Build Guide

## 📦 Building the .exe File

### **🔧 Prerequisites:**
- ✅ **Visual Studio 2022** or **.NET 9.0 SDK**
- ✅ **Windows 10/11** development environment
- ✅ **All project dependencies** installed

### **🚀 Quick Build Steps:**

#### **Option 1: Using Batch Script**
```bash
# Run the batch script
build-exe.bat
```

#### **Option 2: Using PowerShell**
```powershell
# Run the PowerShell script
.\build-exe.ps1
```

#### **Option 3: Manual Build**
```bash
# Clean previous builds
dotnet clean

# Build Release version
dotnet build --configuration Release

# Publish for Windows
dotnet publish --configuration Release --runtime win-x64 --self-contained false
```

## 📁 Output Files

### **✅ After Successful Build:**
- ✅ **`bin\Release\net9.0-windows\CivilProcessERP.exe`** - Main executable
- ✅ **`GLERP-Release\`** folder - Complete distribution package
- ✅ **All required DLLs** and dependencies
- ✅ **README.txt** - Installation instructions

### **📦 Distribution Package Contents:**
```
GLERP-Release/
├── CivilProcessERP.exe          # Main executable
├── CivilProcessERP.dll          # Application library
├── *.dll                        # All required dependencies
├── *.json                       # Configuration files
├── Assets/                      # Application assets
└── README.txt                   # Installation guide
```

## 🚀 Publishing to GitHub Releases

### **Step 1: Create Release Package**
1. **Run build script**: `build-exe.bat`
2. **Zip the GLERP-Release folder**: `GLERP-Release.zip`
3. **Include README.txt** with installation instructions

### **Step 2: Upload to GitHub**
1. **Go to your GitHub repository**
2. **Create new release**: v1.0.0
3. **Upload**: `GLERP-Release.zip`
4. **Set download URL**: `https://github.com/Great-Lakes-Civil-Services/GLCSERP/releases/download/v1.0.0/GLERP-v1.0.0.exe`

### **Step 3: Update Download Page**
- ✅ **Download link** already updated to .exe
- ✅ **File size** updated to ~85 MB
- ✅ **Format** changed to "Windows Executable"

## 📋 Installation Instructions

### **For End Users:**
1. **Download** `GLERP-v1.0.0.exe` from GitHub
2. **Extract** all files to a folder
3. **Double-click** `CivilProcessERP.exe` to run
4. **Ensure** .NET 9.0 Runtime is installed

### **System Requirements:**
- ✅ **Windows 10/11** (64-bit)
- ✅ **4 GB RAM** minimum (8 GB recommended)
- ✅ **500 MB** available disk space
- ✅ **.NET 9.0 Runtime** installed
- ✅ **Internet connection** for database

## 🔧 Build Scripts Created

### **✅ `build-exe.bat`** - Windows Batch Script
- ✅ **Cleans** previous builds
- ✅ **Builds** Release version
- ✅ **Creates** distribution package
- ✅ **Includes** all dependencies

### **✅ `build-exe.ps1`** - PowerShell Script
- ✅ **Same functionality** as batch script
- ✅ **Better error handling**
- ✅ **Colored output**
- ✅ **Cross-platform** compatible

## 🎯 Benefits of .exe Distribution

### **✅ User Experience:**
- ✅ **Single file** download
- ✅ **No extraction** required
- ✅ **Direct execution**
- ✅ **Professional appearance**

### **✅ Technical Advantages:**
- ✅ **Self-contained** with dependencies
- ✅ **No installation** required
- ✅ **Portable** application
- ✅ **Easy distribution**

### **✅ Security:**
- ✅ **Code signing** possible
- ✅ **Digital signature** verification
- ✅ **Trusted execution**
- ✅ **Windows Defender** compatible

## 🚀 Next Steps

### **1. Build the Executable:**
```bash
# Run the build script
build-exe.bat
```

### **2. Test the Build:**
```bash
# Navigate to release folder
cd GLERP-Release

# Run the executable
CivilProcessERP.exe
```

### **3. Create GitHub Release:**
1. **Zip the GLERP-Release folder**
2. **Upload to GitHub Releases**
3. **Update download page link**

### **4. Distribute:**
- ✅ **GitHub Releases** for public download
- ✅ **Direct download** from your website
- ✅ **Email distribution** to clients
- ✅ **USB/Network** distribution

## 📊 File Size Optimization

### **Current Size:** ~85 MB
### **Optimization Options:**
- 🔄 **Self-contained** build (larger but standalone)
- 🔄 **Framework-dependent** build (smaller, requires .NET)
- 🔄 **Trimmed** build (smaller, removes unused code)
- 🔄 **Single file** publish (single .exe)

## 🎯 Ready to Build!

Your GLERP application is ready to be built as a Windows executable. Run the build script and you'll have a professional .exe file ready for distribution!

**Run `build-exe.bat` to create your executable!** 🚀 