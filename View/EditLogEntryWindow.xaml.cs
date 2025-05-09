using System;
using System.Windows;
using CivilProcessERP.Models;

namespace CivilProcessERP.Views
{
    public partial class EditLogEntryWindow : Window
    {
        public DateTime SelectedDate => datePicker.SelectedDate ?? DateTime.Today;
        public TimeSpan SelectedTime => TimeSpan.Parse(txtTime.Text);
        public string Body => txtBody.Text.Trim();
        public bool Aff => chkAff.IsChecked == true;
        public bool DS => chkDs.IsChecked == true;
        public bool Att => chkAtt.IsChecked == true;
        public string Source => txtSource.Text.Trim();

        public EditLogEntryWindow(LogEntryModel entry)
        {
            InitializeComponent();

            if (entry != null)
            {
                datePicker.SelectedDate = entry.Date;
                txtTime.Text = entry.Time ?? "00:00:00";
                txtBody.Text = entry.Body ?? "";
                chkAff.IsChecked = entry.Aff;
                chkDs.IsChecked = entry.FS;
                chkAtt.IsChecked = entry.Att;
                txtSource.Text = entry.Source ?? "";
            }
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            if (!DateTime.TryParse(SelectedTime.ToString(), out _))
            {
                MessageBox.Show("Please enter valid time (e.g. 14:30:00)");
                return;
            }

            DialogResult = true;
            Close();
        }
    }
}
