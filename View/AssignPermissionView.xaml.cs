using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using CivilProcessERP.Models;
using CivilProcessERP.Services;

namespace CivilProcessERP.Views
{
    public partial class AssignPermissionView : System.Windows.Controls.UserControl
    {
        private readonly UserSearchService _userService = new();
        private readonly UserPermissionService _userPermissionService = new();
        private readonly GroupPermissionService _permissionService = new(); // Loads all available permissions

        private List<UserModel> _allUsers = new();

        public AssignPermissionView()
        {
            InitializeComponent();
            _ = LoadUsersAsync();
            _ = LoadAllPermissionsAsync();
        }

        private async Task LoadUsersAsync()
        {
            try
            {
                _allUsers = await Task.Run(() => _userService.GetAllUsersAsync()).ConfigureAwait(false);

                Dispatcher.Invoke(() =>
                {
                    UserComboBox.ItemsSource = _allUsers;
                    UserComboBox.DisplayMemberPath = "LoginName";
                });
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() => System.Windows.MessageBox.Show("Failed to load users: " + ex.Message));
            }
        }

        private async Task LoadAllPermissionsAsync()
        {
            try
            {
                var allPerms = await Task.Run(() => _permissionService.GetAllPermissionsAsync()).ConfigureAwait(false);

                Dispatcher.Invoke(() => PermissionListBox.ItemsSource = allPerms);
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() => System.Windows.MessageBox.Show("Failed to load permissions: " + ex.Message));
            }
        }

        private async void UserComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (UserComboBox.SelectedItem is not UserModel user)
                return;

            try
            {
                var directPermissions = await Task.Run(() =>
                    _userPermissionService.GetPermissionsForUserAsync(user.UserNumber)).ConfigureAwait(false);

                Dispatcher.Invoke(() =>
                {
                    PermissionListBox.SelectedItems.Clear();
                    foreach (string perm in PermissionListBox.Items)
                    {
                        if (directPermissions.Contains(perm))
                            PermissionListBox.SelectedItems.Add(perm);
                    }
                });
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() => System.Windows.MessageBox.Show("Failed to load user permissions: " + ex.Message));
            }
        }

        private async void SavePermissions_Click(object sender, RoutedEventArgs e)
        {
            if (UserComboBox.SelectedItem is not UserModel user)
            {
                System.Windows.MessageBox.Show("Select a user first.");
                return;
            }

            try
            {
                var selectedPerms = PermissionListBox.SelectedItems.Cast<string>().ToList();

                await Task.Run(() =>
                    _userPermissionService.SavePermissionsForUserAsync(user.UserNumber, selectedPerms)).ConfigureAwait(false);

                Dispatcher.Invoke(() => System.Windows.MessageBox.Show("Permissions saved directly to user."));
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Failed to save permissions: " + ex.Message);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            UserComboBox.SelectedItem = null;
            PermissionListBox.SelectedItems.Clear();
        }
    }
}
