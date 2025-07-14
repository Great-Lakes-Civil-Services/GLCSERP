using System;
using System.Windows;

namespace CivilProcessERP.Views
{
    public partial class EditDateDialog : Window
    {
        public DateTime? SelectedDate => datePicker.SelectedDate;

        public EditDateDialog(string fieldTitle, DateTime? currentDate)
        {
            InitializeComponent();
            lblTitle.Text = $"Edit {fieldTitle}";
            datePicker.SelectedDate = currentDate ?? DateTime.Today;
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedDate == null)
            {
                System.Windows.MessageBox.Show("Please select a valid date.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            DialogResult = true;
            Close();
        }
    }
}
