using System;
using System.Windows;
using System.Windows.Controls;

namespace CivilProcessERP.Views
{
    public partial class EditDateDialog : Window
    {
        //private DatePicker datePicker = new DatePicker();
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
                MessageBox.Show("Please select a valid date.");
                return;
            }

            DialogResult = true;
            Close();
        }
    }
}