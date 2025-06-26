using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using CivilProcessERP.Services;

namespace CivilProcessERP.Views
{
    public partial class DeleteGroupView : UserControl
    {
        private readonly GroupService _groupService = new();

        public DeleteGroupView()
        {
            InitializeComponent();
            _ = LoadGroupsAsync();
        }

        private async Task LoadGroupsAsync()
        {
            var groups = await Task.Run(() => _groupService.GetAllGroupNamesAsync());
            GroupComboBox.ItemsSource = groups;
            GroupComboBox.SelectedIndex = groups.Count > 0 ? 0 : -1;
        }

        private async void Delete_Click(object sender, RoutedEventArgs e)
        {
            StatusText.Visibility = Visibility.Collapsed;
            StatusText.Text = "";

            if (GroupComboBox.SelectedItem is not string groupName)
            {
                StatusText.Text = "⚠ Please select a group to delete.";
                StatusText.Foreground = Brushes.Red;
                StatusText.Visibility = Visibility.Visible;
                return;
            }

            var confirm = MessageBox.Show(
                $"Are you sure you want to delete group '{groupName}'?",
                "Confirm Deletion",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (confirm == MessageBoxResult.Yes)
            {
                bool deleted = await Task.Run(() => _groupService.DeleteGroupAsync(groupName));
                if (deleted)
                {
                    StatusText.Foreground = Brushes.Green;
                    StatusText.Text = $"✅ Group '{groupName}' deleted successfully.";
                }
                else
                {
                    StatusText.Foreground = Brushes.Red;
                    StatusText.Text = $"❌ Failed to delete group '{groupName}'.";
                }

                StatusText.Visibility = Visibility.Visible;
                await LoadGroupsAsync(); // 🔄 Refresh list after deletion
            }
        }
    }
}
