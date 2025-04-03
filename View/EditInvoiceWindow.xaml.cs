using System.Windows;
using CivilProcessERP.Models;
using CivilProcessERP.Models.Job;

namespace CivilProcessERP.Views
{
    public partial class EditInvoiceWindow : Window
    {
        public EditInvoiceWindow(InvoiceEntryModel invoice)
        {
            InitializeComponent();
            DataContext = invoice;
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
