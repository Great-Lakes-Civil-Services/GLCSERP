using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using CivilProcessERP.Services;

namespace CivilProcessERP.Views
{
    public partial class ManagePermissionsView : UserControl
    {
        private readonly GroupPermissionService _permService = new();

        public ManagePermissionsView()
        {
            InitializeComponent();
            Loaded += async (_, __) => await RefreshPermissionListAsync();
        }

        private async Task RefreshPermissionListAsync()
        {
            try
            {
                var permissions = await _permService.GetAllPermissionsAsync();
                PermissionListBox.ItemsSource = permissions;
                PermissionListBox.SelectedIndex = -1;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load permissions: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void Add_Click(object sender, RoutedEventArgs e)
        {
            StatusText.Visibility = Visibility.Collapsed;
            StatusText.Text = "";

            string newPerm = NewPermissionBox.Text.Trim();

            if (string.IsNullOrEmpty(newPerm))
            {
                StatusText.Text = "⚠ Please enter a permission name.";
                StatusText.Visibility = Visibility.Visible;
                return;
            }

            try
            {
                bool added = await _permService.AddPermissionAsync(newPerm);

                if (added)
                {
                    StatusText.Foreground = System.Windows.Media.Brushes.Green;
                    StatusText.Text = $"✅ Permission '{newPerm}' added successfully.";
                }
                else
                {
                    StatusText.Foreground = System.Windows.Media.Brushes.Red;
                    StatusText.Text = $"❌ Failed to add. Permission may already exist.";
                }

                StatusText.Visibility = Visibility.Visible;
                NewPermissionBox.Clear();
                await RefreshPermissionListAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to add permission: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void Delete_Click(object sender, RoutedEventArgs e)
        {
            StatusText.Visibility = Visibility.Collapsed;
            StatusText.Text = "";

            if (PermissionListBox.SelectedItem is not string selected)
            {
                StatusText.Text = "⚠ Please select a permission to delete.";
                StatusText.Visibility = Visibility.Visible;
                return;
            }

            var confirm = MessageBox.Show($"Are you sure you want to delete permission '{selected}'?", "Confirm Deletion", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (confirm != MessageBoxResult.Yes) return;

            try
            {
                bool deleted = await _permService.DeletePermissionAsync(selected);

                if (deleted)
                {
                    StatusText.Foreground = System.Windows.Media.Brushes.Green;
                    StatusText.Text = $"✅ Permission '{selected}' deleted successfully.";
                }
                else
                {
                    StatusText.Foreground = System.Windows.Media.Brushes.Red;
                    StatusText.Text = $"❌ Failed to delete permission '{selected}'.";
                }

                StatusText.Visibility = Visibility.Visible;
                await RefreshPermissionListAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to delete permission: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void Cancel_Click(object sender, RoutedEventArgs e)
        {
            NewPermissionBox.Clear();
            PermissionListBox.SelectedIndex = -1;
            StatusText.Text = "";
            StatusText.Visibility = Visibility.Collapsed;
            await RefreshPermissionListAsync();
        }
    }
}
