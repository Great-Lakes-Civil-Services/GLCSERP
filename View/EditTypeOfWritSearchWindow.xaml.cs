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

        public bool IsNewTypeOfWrit { get; set; } = false;
        public string NewTypeOfWritFullName { get; set; } = string.Empty;

        private readonly string _connectionString;

        public EditTypeOfWritSearchWindow(string connectionString, string initialValue = "")
        {
            _connectionString = connectionString;
            try
            {
                System.Diagnostics.Debug.WriteLine("[DEBUG] Entering EditTypeOfWritSearchWindow constructor");
                InitializeComponent();
                System.Diagnostics.Debug.WriteLine("[DEBUG] InitializeComponent called");
                System.Diagnostics.Debug.WriteLine($"[DEBUG] txtSearch is {(txtSearch == null ? "null" : "not null")}");
                System.Diagnostics.Debug.WriteLine($"[DEBUG] lstTypes is {(lstTypes == null ? "null" : "not null")}");
                txtSearch.Text = initialValue;
                try
                {
                    _availableTypes = FetchWritTypesFromDb(_connectionString);
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] _availableTypes count: {_availableTypes?.Count}");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[ERROR] Exception in FetchWritTypesFromDb: {ex}");
                    _availableTypes = new List<string>();
                }
                LoadTypes(initialValue);
                System.Diagnostics.Debug.WriteLine("[DEBUG] LoadTypes called");
                if (lstTypes == null)
                    System.Diagnostics.Debug.WriteLine("[ERROR] lstTypes is null after InitializeComponent!");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] Exception in constructor: {ex}");
                throw;
            }
        }

        private List<string> FetchWritTypesFromDb(string connStr)
        {
            var types = new List<string>();
            try
            {
                System.Diagnostics.Debug.WriteLine("[DEBUG] Opening DB connection for writ types");
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
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Loaded {types.Count} writ types from DB");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] Failed to load writ types from DB: {ex.Message}");
            }
            return types;
        }

        private void txtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("[DEBUG] txtSearch_TextChanged called");
            LoadTypes(txtSearch.Text.Trim());
        }

        private void LoadTypes(string filter)
        {
            if (lstTypes == null)
            {
                System.Diagnostics.Debug.WriteLine("[ERROR] lstTypes is null in LoadTypes!");
                return;
            }
            if (_availableTypes == null)
            {
                System.Diagnostics.Debug.WriteLine("[ERROR] _availableTypes is null in LoadTypes!");
                return;
            }
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
            var inputDialog = new SingleFieldDialog("Enter Type of Writ", "");
            if (inputDialog.ShowDialog() == true)
            {
                IsNewTypeOfWrit = true;
                NewTypeOfWritFullName = inputDialog.Value;
                DialogResult = true;
            }
        }
    }
}
