using System.Windows;
using CivilProcessERP.Models;

namespace CivilProcessERP.Views
{
    public partial class EditLogEntryWindow : Window
    {
        private LogEntryModel _entry;

        public EditLogEntryWindow(LogEntryModel entry)
        {
            InitializeComponent();
            _entry = entry;
            DataContext = _entry;
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true; // This will close the window and indicate success
        }
    }
}
