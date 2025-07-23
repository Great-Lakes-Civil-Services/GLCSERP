# 🚀 GLERP Combined Deployment

## 📁 Files in this folder:
- `index.html` - Registration and download page
- `admin.html` - Admin dashboard
- `test.html` - Data testing page

## 🚀 How to Deploy:

### **Step 1: Deploy to Netlify**
1. **Go to [netlify.com](https://netlify.com)**
2. **Drag this entire folder** to Netlify
3. **Get your site URL**: `https://your-site.netlify.app`

### **Step 2: Access Your Pages**
- **Registration**: `https://your-site.netlify.app/`
- **Admin Dashboard**: `https://your-site.netlify.app/admin.html`
- **Test Page**: `https://your-site.netlify.app/test.html`

## 🔧 Testing Steps:

### **Step 1: Test Data Storage**
1. **Go to**: `https://your-site.netlify.app/test.html`
2. **Click "Add Test Data"**
3. **Check if data appears** in the test page

### **Step 2: Test Registration**
1. **Go to**: `https://your-site.netlify.app/`
2. **Fill out the form** and download
3. **Check browser console** (F12) for "Download tracked and stored"

### **Step 3: Check Admin Dashboard**
1. **Go to**: `https://your-site.netlify.app/admin.html`
2. **Refresh the page**
3. **Check if data appears** in the table

## 🎯 Why This Works:
- ✅ **Same domain** = Data sharing works
- ✅ **localStorage** is shared between pages
- ✅ **Admin dashboard** can read registration data

## 🔍 If Still No Data:
1. **Check browser console** (F12) for errors
2. **Try the test page** first
3. **Clear browser cache** and try again
4. **Use incognito mode** to test fresh 