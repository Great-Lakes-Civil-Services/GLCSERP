using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace CivilProcessERP.Views
{
    public partial class EditTypeOfWritSearchWindow : Window
    {
        public string SelectedTypeOfWrit => lstTypes.SelectedItem?.ToString() ?? "";

        private readonly List<string> _availableTypes = new()
        {
            "SUMMONS AND COMPLAINT",
            "WARRANT OF EVICTION",
            "WRIT OF RESTITUTION",
            "NOTICE TO QUIT",
            "NOTICE TO VACATE",
            "COURT SUMMONS",
            "MOTION FOR CONTEMPT",
            "SHOW CAUSE ORDER"
            // ✅ Add more types if needed
        };

        public EditTypeOfWritSearchWindow(string initialValue = "")
        {
            InitializeComponent();
            txtSearch.Text = initialValue;
            LoadTypes(initialValue);
        }

        private void txtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            LoadTypes(txtSearch.Text.Trim());
        }

        private void LoadTypes(string filter)
        {
            lstTypes.Items.Clear();
            foreach (var type in _availableTypes)
            {
                if (string.IsNullOrWhiteSpace(filter) || type.ToLower().Contains(filter.ToLower()))
                    lstTypes.Items.Add(type);
            }

            if (!string.IsNullOrWhiteSpace(filter))
            {
                foreach (var item in lstTypes.Items)
                {
                    if (item.ToString().Equals(filter, System.StringComparison.OrdinalIgnoreCase))
                    {
                        lstTypes.SelectedItem = item;
                        lstTypes.ScrollIntoView(item);
                        break;
                    }
                }
            }
        }

        private void lstTypes_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (lstTypes.SelectedItem != null)
            {
                DialogResult = true;
                Close();
            }
        }

        private void Select_Click(object sender, RoutedEventArgs e)
        {
            if (lstTypes.SelectedItem != null)
            {
                DialogResult = true;
                Close();
            }
            else
            {
                MessageBox.Show("Please select a Type of Writ.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
