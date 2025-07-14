using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using CivilProcessERP.Services;
using CivilProcessERP.Models;

namespace CivilProcessERP.Views
{
    public partial class RolePermissionManagerView : System.Windows.Controls.UserControl
    {
        private readonly RolePermissionService _rolePermService = new();
        private Dictionary<string, int> _roleMap = new();  // rolename -> rolenumber
        private List<string> _allPermissions = new();

        public RolePermissionManagerView()
        {
            InitializeComponent();
            Loaded += async (_, __) =>
            {
                await LoadRolesAsync();
                await LoadAllPermissionsAsync();
            };
        }

        private async Task LoadRolesAsync()
        {
            try
            {
                _roleMap = await _rolePermService.GetAllRolesAsync();
                RoleComboBox.ItemsSource = _roleMap.Keys;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Failed to load roles: {ex.Message}", "Error", MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private async Task LoadAllPermissionsAsync()
        {
            try
            {
                _allPermissions = await _rolePermService.GetAllPermissionsAsync();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Failed to load permissions: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private async void RoleComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (RoleComboBox.SelectedItem is not string roleName || !_roleMap.TryGetValue(roleName, out int roleId))
                return;

            try
            {
                var assignedPermissions = (await _rolePermService.GetPermissionsForRoleAsync(roleId))
                                            .Select(p => p.Permission)
                                            .ToHashSet();

                PermissionsListBox.ItemsSource = _allPermissions;
                PermissionsListBox.SelectedItems.Clear();

                foreach (string perm in _allPermissions)
                {
                    if (assignedPermissions.Contains(perm))
                        PermissionsListBox.SelectedItems.Add(perm);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Failed to load assigned permissions: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private async void SavePermissions_Click(object sender, RoutedEventArgs e)
        {
            if (RoleComboBox.SelectedItem is not string roleName || !_roleMap.TryGetValue(roleName, out int roleId))
            {
                System.Windows.MessageBox.Show("Please select a valid role.");
                return;
            }

            var selectedPermissions = PermissionsListBox.SelectedItems.Cast<string>().ToList();

            try
            {
                await _rolePermService.SavePermissionsForRoleAsync(roleId, selectedPermissions);
                System.Windows.MessageBox.Show($"✅ Permissions updated for role: {roleName}");
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Failed to save permissions: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private async void Cancel_Click(object sender, RoutedEventArgs e)
        {
            RoleComboBox.SelectedIndex = -1;
            PermissionsListBox.ItemsSource = null;
            PermissionsListBox.SelectedItems.Clear();
            await LoadAllPermissionsAsync();
        }
    }
}
