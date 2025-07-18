using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System;
using Npgsql;

namespace CivilProcessERP.Views
{
    public partial class EditTypeOfWritSearchWindow : Window
    {
        public string SelectedTypeOfWrit => lstTypes.SelectedItem?.ToString() ?? "";

        private List<string> _availableTypes;

        public EditTypeOfWritSearchWindow(string initialValue = "")
        {
            InitializeComponent();
            txtSearch.Text = initialValue;
            _availableTypes = FetchWritTypesFromDb();
            if (_availableTypes.Count == 0)
            {
                // Fallback to old hardcoded list if DB is empty
                _availableTypes = new List<string>
                {
                    "SUMMONS AND COMPLAINT",
                    "WARRANT OF EVICTION",
                    "WRIT OF RESTITUTION",
                    "NOTICE TO QUIT",
                    "NOTICE TO VACATE",
                    "COURT SUMMONS",
                    "MOTION FOR CONTEMPT",
                    "SHOW CAUSE ORDER"
                };
            }
            LoadTypes(initialValue);
        }

        private List<string> FetchWritTypesFromDb()
        {
            var types = new List<string>();
            string connStr = "Host=localhost;Port=5432;Database=mypg_database;Username=postgres;Password=7866";
            try
            {
                using var conn = new Npgsql.NpgsqlConnection(connStr);
                conn.Open();
                using var cmd = new Npgsql.NpgsqlCommand("SELECT DISTINCT typewrit FROM plongs WHERE typewrit IS NOT NULL AND typewrit <> '' ORDER BY typewrit", conn);
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    var type = reader["typewrit"]?.ToString();
                    if (!string.IsNullOrWhiteSpace(type))
                        types.Add(type);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] Failed to load writ types from DB: {ex.Message}");
            }
            return types;
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
                System.Windows.MessageBox.Show("Please select a Type of Writ.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void Other_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new CivilProcessERP.Views.SingleFieldDialog("Type of Writ", txtSearch.Text);
            if (dialog.ShowDialog() == true)
            {
                string newType = dialog.Value;
                if (!string.IsNullOrWhiteSpace(newType))
                {
                    lstTypes.SelectedItem = null;
                    txtSearch.Text = newType;
                    if (!lstTypes.Items.Contains(newType))
                        lstTypes.Items.Insert(0, newType);
                    DialogResult = true;
                    Close();
                }
            }
        }
    }
}
