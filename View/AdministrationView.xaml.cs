using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input; // Added for KeyEventArgs
using System.Windows.Controls; // Added for Selector
using System.Windows.Controls.Primitives; // Added for Selector.SelectionChangedEvent
using CivilProcessERP.Services;
using CivilProcessERP.Models; // Added for UserModel
using CivilProcessERP.Services;
using System.Threading.Tasks;

namespace CivilProcessERP.Views
{
    public partial class AdministrationView : UserControl
    {


    private readonly string _connString = "Host=localhost;Port=5432;Username=postgres;Password=7866;Database=mypg_database";

        private readonly UserSearchService _userSearchService = new UserSearchService();
        private readonly GroupService _groupService = new();
        private HashSet<string> _loggedInUserPermissions = new(); // Only for controlling visibility

        private readonly UserModel _currentUser;


        private readonly GroupPermissionService _groupPermissionService = new GroupPermissionService();


        private List<UserModel> _searchResults = new List<UserModel>();



        public AdministrationView(UserModel currentUser)
        {
            InitializeComponent();
            _currentUser = currentUser;
            _auditLogService = new AuditLogService( _connString);
            Console.WriteLine("[INFO] AdministrationView Initialized for " + _currentUser.LoginName);
            _ = LoadCurrentUserPermissionsAsync();
        }

        private void ApplyPermissionBasedVisibility()
        {
            CreateUserButton.Visibility = _loggedInUserPermissions.Contains("AddUser") ? Visibility.Visible : Visibility.Collapsed;
            EditUserButton.Visibility = _loggedInUserPermissions.Contains("EditUser") ? Visibility.Visible : Visibility.Collapsed;
            DeleteUserButton.Visibility = _loggedInUserPermissions.Contains("DeleteUser") ? Visibility.Visible : Visibility.Collapsed;
            ToggleUserStatusButton.Visibility = _loggedInUserPermissions.Contains("ToggleUserStatus") ? Visibility.Visible : Visibility.Collapsed;
            AssignPermissionsButton.Visibility = _loggedInUserPermissions.Contains("AssignGroups") ? Visibility.Visible : Visibility.Collapsed;
            ManageOrgsButton.Visibility = _loggedInUserPermissions.Contains("ToggleOrganizationStatus") ? Visibility.Visible : Visibility.Collapsed;
            DeleteGroupButton.Visibility = _loggedInUserPermissions.Contains("DeleteGroups") ? Visibility.Visible : Visibility.Collapsed;
            ManagePermissionsButton.Visibility = _loggedInUserPermissions.Contains("AddPermissions") ? Visibility.Visible : Visibility.Collapsed;
            ManageRolePermissionsButton.Visibility = _loggedInUserPermissions.Contains("ManageRolePermissions") ? Visibility.Visible : Visibility.Collapsed;
}

        // ✅ Navigate to CreateUserView as a new full tab/page
        private async void CreateUserButton_Click(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("[DEBUG] Create User button clicked — navigating to new tab...");
            var mainWindow = Application.Current.MainWindow as MainWindow;
            if (mainWindow != null)
            {
                var createUserPage = new CreateUserView();
                mainWindow.AddNewTab(createUserPage, "Create User");
            }
            else
            {
                MessageBox.Show("❌ Main window is not available.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Console.WriteLine("[ERROR] MainWindow reference is null.");
            }
        }

        // ✅ Navigate to EditUserView as a new full tab/page
       private async void EditUserButton_Click(object sender, RoutedEventArgs e)
{
    Console.WriteLine("[DEBUG] Edit User button clicked — navigating via MainContentArea...");
    var mainWindow = Application.Current.MainWindow as MainWindow;
    if (mainWindow != null)
    {
        string loginInput = UserSearchComboBox.Text.Trim();
        var matchedUser = _searchResults.FirstOrDefault(u =>
            u.LoginName.Equals(loginInput, StringComparison.OrdinalIgnoreCase));
        if (matchedUser != null)
        {
            var editUserPage = new EditUserView(matchedUser);
            mainWindow.AddNewTab(editUserPage, $"Edit: {matchedUser.LoginName}");
        }
        else
        {
            MessageBox.Show("⚠ Please type or select a valid user before editing.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
    else
    {
        MessageBox.Show("Main window is not available.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
    }
}



        // ❌ Not used in tabbed mode
        private void CancelCreateUser_Click(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("[DEBUG] CancelCreateUser_Click — no longer applicable in tabbed mode.");
        }

        private async void DeleteUserButton_Click(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("[DEBUG] Delete User button clicked — navigating to DeleteUserView...");
            var mainWindow = Application.Current.MainWindow as MainWindow;
            if (mainWindow != null)
            {
                var deleteUserPage = new DeleteUserView();
                mainWindow.AddNewTab(deleteUserPage, "Delete User");
            }
            else
            {
                MessageBox.Show("Main window is not available.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void ToggleUserStatusButton_Click(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("[DEBUG] Toggle User Status clicked — navigating to view...");
            var mainWindow = Application.Current.MainWindow as MainWindow;
            if (mainWindow != null)
            {
                var togglePage = new EnableDisableUserView();
                mainWindow.AddNewTab(togglePage, "Enable/Disable User");
            }
            else
            {
                MessageBox.Show("Main window not found.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private async void AssignPermissionsButton_Click(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("[DEBUG] AssignPermissionsButton clicked — opening tab...");
            var mainWindow = Application.Current.MainWindow as MainWindow;
            if (mainWindow != null)
            {
                mainWindow.AddNewTab(new AssignPermissionView(), "Assign Permissions");
            }
            else
            {
                MessageBox.Show("Main window not found.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void FreezeOrgButton_Click(object sender, RoutedEventArgs e)
        {
            var mainWindow = Application.Current.MainWindow as MainWindow;
            if (mainWindow != null)
            {
                mainWindow.AddNewTab(new FreezeOrganizationView(), "Manage Organization Freeze Status");
            }
            else
            {
                MessageBox.Show("Main window not found.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private readonly AuditLogService _auditLogService;
        //private readonly AuditLogService _auditLogService = new AuditLogService(_connStr1);

        private async Task LoadAuditLogsAsync()
        {
            var logs = await _auditLogService.GetLogsAsync();
            UserAuditLogDataGrid.ItemsSource = logs;
        }

        private async void UserSearchComboBox_KeyUp(object sender, KeyEventArgs e)
        {
            string query = UserSearchComboBox.Text.Trim();
            if (query.Length >= 2)
            {
                _searchResults = await _userSearchService.SearchUsersAsync(query);
                UserSearchComboBox.ItemsSource = _searchResults;
                UserSearchComboBox.IsDropDownOpen = true;
            }
        }


        private async void UserSearchComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (UserSearchComboBox.SelectedItem is UserModel selectedUser)
            {
                Console.WriteLine($"[INFO] Selected user: {selectedUser.LoginName}");
                var userLogs = await _auditLogService.GetLogsForUserAsync(selectedUser.LoginName);
                UserAuditLogDataGrid.ItemsSource = userLogs;
                var rolePermissionService = new RolePermissionService();
                int roleId = selectedUser.RoleNumber;
                var permissions = await rolePermissionService.GetPermissionsForRoleAsync(roleId);
                SelectedUserPermissionsGrid.ItemsSource = permissions;
                Console.WriteLine("[DEBUG] Reloading all users for user assignment list...");
                await LoadAllUsersAsync();
                Console.WriteLine("[DEBUG] Reloading all permissions...");
                await LoadAllPermissionsAsync();
                Console.WriteLine("[DEBUG] Refreshing group list and selection...");
                await RefreshGroupListsAsync();
                MfaEnabledCheckBox.IsChecked = selectedUser.MfaEnabled;
                LastLoginTextBox.Text = selectedUser.MfaLastVerifiedAt?.ToLocalTime().ToString("g") ?? "N/A";
                if (selectedUser.MfaLastVerifiedAt.HasValue)
                {
                    var timeSinceMfa = DateTime.UtcNow - selectedUser.MfaLastVerifiedAt.Value;
                    if (timeSinceMfa.TotalMinutes <= 60)
                    {
                        Console.WriteLine($"[INFO] MFA was verified {timeSinceMfa.TotalMinutes:F1} minutes ago — skipping MFA re-verification.");
                        SkipMfaVerificationUI();
                    }
                    else
                    {
                        Console.WriteLine("[INFO] MFA verification is older than 1 hour — MFA step required.");
                        RequireMfaVerificationUI();
                    }
                }
                else
                {
                    Console.WriteLine("[INFO] No MFA verification timestamp found — MFA step required.");
                    RequireMfaVerificationUI();
                }
                var apiService = new ApiCredentialService(_connString);
                var creds = await apiService.GetCredentialAsync(selectedUser.UserNumber);
                if (creds != null)
                {
                    AccessKeyTextBox.Text = creds.AccessKey;
                    SecretKeyTextBox.Text = creds.SecretKey;
                }
                else
                {
                    AccessKeyTextBox.Text = "";
                    SecretKeyTextBox.Text = "";
                }
                Console.WriteLine($"[SUCCESS] Finished loading all information for user: {selectedUser.LoginName}");
            }
        }

        private void SkipMfaVerificationUI()
{
    // Hide MFA verification panel or disable its requirement
    if (FindName("MfaVerificationPanel") is UIElement panel)
        panel.Visibility = Visibility.Collapsed;
}

        private void RequireMfaVerificationUI()
        {
            // Show MFA verification panel or indicate it's required
            if (FindName("MfaVerificationPanel") is UIElement panel)
                panel.Visibility = Visibility.Visible;
        }

private async void DeleteGroupButton_Click(object sender, RoutedEventArgs e)
{
    var mainWindow = Application.Current.MainWindow as MainWindow;
    mainWindow?.AddNewTab(new DeleteGroupView(), "Delete Group");
}

        private async void ManagePermissionsButton_Click(object sender, RoutedEventArgs e)
        {
            var mainWindow = Application.Current.MainWindow as MainWindow;
            mainWindow?.AddNewTab(new ManagePermissionsView(), "Manage Permissions");
        }

private async void ManageRolePermissionsButton_Click(object sender, RoutedEventArgs e)
{
    Console.WriteLine("[DEBUG] ManageRolePermissionsButton clicked — opening RolePermissionManagerView...");
    var mainWindow = Application.Current.MainWindow as MainWindow;
    if (mainWindow != null)
    {
        mainWindow.AddNewTab(new RolePermissionManagerView(), "Manage Role Permissions");
    }
    else
    {
        MessageBox.Show("Main window not found.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
    }
}




        private readonly PermissionTemplateService _templateService = new();

        private async Task LoadPermissionTemplatesAsync()
        {
            var templates = await _templateService.GetAllTemplateNamesAsync();
            PermissionTemplateComboBox.ItemsSource = templates;
        }
 private async void UserControl_Loaded(object sender, RoutedEventArgs e)
{
    await LoadAuditLogsAsync();
    await LoadPermissionTemplatesAsync();
    await LoadAllUsersAsync();
    await LoadAllPermissionsAsync();
    await LoadCurrentUserPermissionsAsync();
    SelectedUserPermissionsGrid.ItemsSource = _loggedInUserPermissions
        .Select(p => new PermissionModel { Permission = p, IsGranted = true })
        .ToList();
    Dispatcher.BeginInvoke(new Action(async () =>
    {
        await RefreshGroupListsAsync();
        if (GroupListBox.Items.Count > 0)
        {
            GroupListBox.SelectedIndex = 0;
            if (GroupListBox.SelectedItem is string selectedGroup)
            {
                await LoadGroupDetailsAsync(selectedGroup);
            }
        }
    }), System.Windows.Threading.DispatcherPriority.Background);
}



private async Task LoadCurrentUserPermissionsAsync()
{
    var rolePermissionService = new RolePermissionService();
    var userPermissionService = new UserPermissionService();
    var rolePerms = (await rolePermissionService
        .GetPermissionsForRoleAsync(_currentUser.RoleNumber))
        .Select(p => p.Permission);
    var userPerms = await userPermissionService.GetPermissionsForUserAsync(_currentUser.UserNumber);
    _loggedInUserPermissions = new HashSet<string>(rolePerms.Concat(userPerms));
    Console.WriteLine($"[DEBUG] Logged-in permissions for {_currentUser.LoginName}: {string.Join(", ", _loggedInUserPermissions)}");
    ApplyPermissionBasedVisibility();
}




        private List<UserModel> _allUsers = new(); // ⬅️ Keep reference to all users

        private async Task LoadAllUsersAsync()
        {
            _allUsers = await _userSearchService.GetAllUsersAsync();
            if (_allUsers == null || _allUsers.Count == 0)
            {
                Console.WriteLine("[WARN] No users returned from GetAllUsersAsync()]");
            }
            else
            {
                Console.WriteLine($"[INFO] Loaded {_allUsers.Count} users.");
            }
            UserAssignmentListBox.ItemsSource = _allUsers;
            UserAssignmentListBox.DisplayMemberPath = "LoginName"; // ✅ show names
        }



        private async void ApplyTemplateButton_Click(object sender, RoutedEventArgs e)
        {
            if (PermissionTemplateComboBox.SelectedItem is string selectedTemplate)
            {
                var permissions = await _templateService.GetPermissionsForTemplateAsync(selectedTemplate);
                if (GroupListBox.SelectedItem is string selectedGroup)
                {
                    await _groupPermissionService.SavePermissionsForGroupAsync(selectedGroup, permissions);
                    MessageBox.Show($"Applied '{selectedTemplate}' to group '{selectedGroup}' successfully.");
                }
                SelectedUserPermissionsGrid.ItemsSource = permissions.Select(p => new PermissionModel
                {
                    Permission = p,
                    IsGranted = true
                }).ToList();
            }
        }

        private async void CreateGroupButton_Click(object sender, RoutedEventArgs e)
        {
            var groupName = NewGroupNameTextBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(groupName))
            {
                MessageBox.Show("Please enter a valid group name.");
                return;
            }
            bool created = await _groupService.CreateGroupAsync(groupName);
            if (created)
            {
                MessageBox.Show("Group created successfully.");
                NewGroupNameTextBox.Clear();
                await RefreshGroupListsAsync();
            }
            else
            {
                MessageBox.Show("Group already exists.");
            }
        }


        private async Task RefreshGroupListsAsync()
        {
            var allGroups = await _groupService.GetAllGroupNamesAsync();
            Console.WriteLine("[DEBUG] Groups loaded: " + string.Join(", ", allGroups));
            GroupListBox.ItemsSource = allGroups;
            if (allGroups.Any())
            {
                GroupListBox.SelectedIndex = 0;
                string selectedGroup = allGroups[0];
                Console.WriteLine("[DEBUG] Default group selected: " + selectedGroup);
                await LoadGroupDetailsAsync(selectedGroup);
            }
            else
            {
                Console.WriteLine("[WARN] No groups available.");
            }
        }



        private async Task LoadGroupDetailsAsync(string groupName)
        {
            // Load and highlight permissions
            var assignedPermissions = await _groupPermissionService.GetPermissionsForGroupAsync(groupName);
            var allPermissions = await _groupPermissionService.GetAllPermissionsAsync();

            GroupPermissionsListBox.ItemsSource = allPermissions;
            GroupPermissionsListBox.SelectedItems.Clear();
            foreach (var perm in assignedPermissions)
            {
                if (allPermissions.Contains(perm))
                    GroupPermissionsListBox.SelectedItems.Add(perm);
            }

            // Load and highlight users
            var usersInGroup = await _groupService.GetUsersForGroupAsync(groupName);
            UserAssignmentListBox.SelectedItems.Clear();
            foreach (var user in _allUsers)
            {
                if (usersInGroup.Any(u => u.UserNumber == user.UserNumber))
                    UserAssignmentListBox.SelectedItems.Add(user);
            }
        }


        private async void GroupListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (GroupListBox.SelectedItem is string groupName)
            {
                await LoadGroupDetailsAsync(groupName);
            }
        }

        private async Task LoadAllPermissionsAsync()
        {
            var allPerms = await _groupPermissionService.GetAllPermissionsAsync();
            GroupPermissionsListBox.ItemsSource = allPerms;
        }




        private async void SaveGroupUsersPermissions_Click(object sender, RoutedEventArgs e)
        {
            string groupName = GroupListBox.SelectedItem as string;
            if (string.IsNullOrWhiteSpace(groupName))
            {
                MessageBox.Show("Please select or create a group.");
                return;
            }
            var selectedPermissions = GroupPermissionsListBox?.SelectedItems.Cast<string>().ToList() ?? new List<string>();
            await _groupPermissionService.SavePermissionsForGroupAsync(groupName, selectedPermissions);
            var selectedUsers = UserAssignmentListBox.SelectedItems.Cast<UserModel>().ToList();
            foreach (var user in selectedUsers)
            {
                await _groupService.SaveGroupsForUserAsync(user.UserNumber, new List<string> { groupName });
            }
            MessageBox.Show($"✅ Saved group '{groupName}' with {selectedUsers.Count} users and {selectedPermissions.Count} permissions.");
        }

        private async void SaveUsersToGroup_Click(object sender, RoutedEventArgs e)
        {
            if (GroupListBox.SelectedItem is not string selectedGroup)
            {
                MessageBox.Show("Select a group first.");
                return;
            }
            var selectedUsers = UserAssignmentListBox.SelectedItems.Cast<UserModel>().ToList();
            if (!selectedUsers.Any())
            {
                MessageBox.Show("Select at least one user to assign.");
                return;
            }
            foreach (var user in selectedUsers)
            {
                await _groupService.SaveGroupsForUserAsync(user.UserNumber, new List<string> { selectedGroup });
            }
            MessageBox.Show($"Assigned {selectedUsers.Count} users to group '{selectedGroup}'.");
        }


private async void GenerateNewAccessKey_Click(object sender, RoutedEventArgs e)
{
    if (UserSearchComboBox.SelectedItem is not UserModel selectedUser)
    {
        MessageBox.Show("Please select a user first.");
        return;
    }
    string accessKey = Guid.NewGuid().ToString("N");
    string secretKey = Guid.NewGuid().ToString("N");
    var credentialService = new ApiCredentialService(_connString);
    await credentialService.CreateOrUpdateCredentialAsync(selectedUser.UserNumber, accessKey, secretKey);
    AccessKeyTextBox.Text = accessKey;
    SecretKeyTextBox.Text = secretKey;
    MessageBox.Show($"🔐 API credentials generated for user '{selectedUser.LoginName}'.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
}
    }
}
