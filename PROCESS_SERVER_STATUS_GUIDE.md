# 🔧 Process Server Status Feature Guide

## Overview

The Process Server Status feature allows administrators to designate users as process servers within the GLERP system. This feature provides a simple toggle mechanism similar to the existing Enable/Disable User functionality.

## 🎯 Features

### ✅ **Core Functionality**
- **Toggle Process Server Status**: Enable/disable process server status for any user
- **Database Integration**: Automatic database updates with audit logging
- **Permission-Based Access**: Only users with `ToggleProcessServerStatus` permission can use this feature
- **Real-time Updates**: Immediate status changes with confirmation dialogs
- **Audit Trail**: All changes are logged in the user activity log

### ✅ **User Interface**
- **Purple Button**: "Toggle Process Server" button in Administration view
- **Search Integration**: Works with existing user search functionality
- **Confirmation Dialog**: Prevents accidental status changes
- **Success/Error Messages**: Clear feedback for all operations

## 🗄️ Database Changes

### New Column Added
```sql
ALTER TABLE users 
ADD COLUMN process_server_status BOOLEAN DEFAULT FALSE;
```

### Database Schema
| Column | Type | Default | Description |
|--------|------|---------|-------------|
| `process_server_status` | BOOLEAN | FALSE | Indicates if user is a process server |

### Index Created
```sql
CREATE INDEX idx_users_process_server_status ON users(process_server_status);
```

## 🔧 Implementation Details

### 1. **UserModel Updates**
```csharp
public class UserModel
{
    // ... existing properties ...
    
    // ✅ Process Server Status
    public bool ProcessServerStatus { get; set; }
}
```

### 2. **ProcessServerService**
- **ToggleProcessServerStatusAsync**: Main toggle functionality
- **GetProcessServerStatusAsync**: Retrieve current status
- **GetAllUsersWithProcessServerStatusAsync**: Get all users with status
- **LogProcessServerStatusChangeAsync**: Audit logging

### 3. **Administration View Integration**
- New purple button: "Toggle Process Server"
- Permission-based visibility control
- Integration with existing user search

## 🚀 How to Use

### **For Administrators:**

1. **Navigate to Administration**
   - Go to Administration view
   - Look for the purple "Toggle Process Server" button

2. **Select a User**
   - Type or select a user in the search box
   - Ensure the user exists in the system

3. **Toggle Status**
   - Click "Toggle Process Server" button
   - Confirm the action in the dialog
   - View success/error message

### **For Developers:**

1. **Run Database Migration**
   ```sql
   -- Execute the migration script
   \i Database/AddProcessServerStatusColumn.sql
   ```

2. **Add Permission**
   ```sql
   -- Add permission for administrators
   INSERT INTO permissions (permission_name, description) 
   VALUES ('ToggleProcessServerStatus', 'Allow toggling process server status');
   ```

3. **Assign Permission to Roles**
   ```sql
   -- Example: Assign to admin role
   INSERT INTO role_permissions (role_number, permission) 
   VALUES (1, 'ToggleProcessServerStatus');
   ```

## 📋 API Methods

### **ProcessServerService Methods**

#### `ToggleProcessServerStatusAsync(int userNumber, bool newStatus, string changedBy)`
- **Purpose**: Toggle process server status for a user
- **Parameters**:
  - `userNumber`: The user's ID
  - `newStatus`: New status (true = enabled, false = disabled)
  - `changedBy`: Username of the person making the change
- **Returns**: `bool` - Success status

#### `GetProcessServerStatusAsync(int userNumber)`
- **Purpose**: Get current process server status
- **Parameters**: `userNumber` - The user's ID
- **Returns**: `bool` - Current status

#### `GetAllUsersWithProcessServerStatusAsync()`
- **Purpose**: Get all users with their process server status
- **Returns**: `List<UserModel>` - Users with status

## 🔐 Security & Permissions

### **Required Permission**
- **Permission Name**: `ToggleProcessServerStatus`
- **Description**: Allows toggling process server status
- **Default**: Not assigned to any role (must be explicitly granted)

### **Audit Logging**
All process server status changes are logged with:
- **Action**: `PROCESS_SERVER_STATUS_CHANGE`
- **Detail**: Status change description
- **Changed By**: Username of person making the change
- **Timestamp**: When the change occurred

## 🧪 Testing

### **Manual Testing Steps**

1. **Database Setup**
   ```sql
   -- Run the migration script
   \i Database/AddProcessServerStatusColumn.sql
   ```

2. **Permission Setup**
   ```sql
   -- Add permission
   INSERT INTO permissions (permission_name, description) 
   VALUES ('ToggleProcessServerStatus', 'Allow toggling process server status');
   
   -- Assign to admin role (adjust role_number as needed)
   INSERT INTO role_permissions (role_number, permission) 
   VALUES (1, 'ToggleProcessServerStatus');
   ```

3. **Test Scenarios**
   - ✅ Toggle status for existing user
   - ✅ Toggle status for non-existent user (should show error)
   - ✅ Toggle without permission (button should be hidden)
   - ✅ Verify audit log entries
   - ✅ Verify database updates

### **Automated Testing**
```csharp
[Test]
public async Task ToggleProcessServerStatus_ValidUser_ShouldSucceed()
{
    // Arrange
    var service = new ProcessServerService();
    var userNumber = 1;
    var newStatus = true;
    var changedBy = "admin";

    // Act
    var result = await service.ToggleProcessServerStatusAsync(userNumber, newStatus, changedBy);

    // Assert
    Assert.IsTrue(result);
}
```

## 🐛 Troubleshooting

### **Common Issues**

1. **Button Not Visible**
   - Check if user has `ToggleProcessServerStatus` permission
   - Verify permission is assigned to user's role

2. **Database Errors**
   - Ensure `process_server_status` column exists
   - Check database connection string
   - Verify user has proper database permissions

3. **No Confirmation Dialog**
   - Check if user is selected in search box
   - Verify user exists in database

### **Debug Information**
```csharp
// Add to ProcessServerService for debugging
Console.WriteLine($"[DEBUG] Toggling process server status for user {userNumber} to {newStatus}");
```

## 📈 Future Enhancements

### **Potential Improvements**
- **Bulk Operations**: Toggle multiple users at once
- **Status Indicators**: Visual indicators in user lists
- **Filtering**: Filter users by process server status
- **Reports**: Generate reports of process servers
- **Notifications**: Notify users when their status changes

### **Integration Opportunities**
- **Job Assignment**: Automatically assign process server jobs
- **Workflow Integration**: Include in process server workflows
- **Reporting**: Include in process server reports
- **Dashboard**: Show process server statistics

## 📞 Support

For issues or questions about the Process Server Status feature:

- **Technical Issues**: Contact development team
- **Database Issues**: Check PostgreSQL logs
- **Permission Issues**: Verify role assignments
- **UI Issues**: Check browser console for errors

---

**Last Updated**: [Current Date]  
**Version**: 1.0.0  
**Author**: GLERP Development Team 