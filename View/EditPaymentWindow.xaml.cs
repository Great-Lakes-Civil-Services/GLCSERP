using System.Windows;
using CivilProcessERP.Models;
using CivilProcessERP.Models.Job;

namespace CivilProcessERP.Views
{
    public partial class EditPaymentWindow : Window
    {
        public EditPaymentWindow(PaymentEntryModel payment)
        {
            InitializeComponent();
            DataContext = payment;
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
