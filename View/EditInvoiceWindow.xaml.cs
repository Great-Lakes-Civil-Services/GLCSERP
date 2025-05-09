using System.Windows;
using CivilProcessERP.Models.Job;   // ✅ fix: import correct namespace

namespace CivilProcessERP.Views
{
    public partial class EditInvoiceWindow : Window
    {
        public EditInvoiceWindow(InvoiceModel invoice)
        {
            InitializeComponent();
            txtDescription.Text = invoice.Description;
            txtQuantity.Text = invoice.Quantity.ToString();
            txtRate.Text = invoice.Rate.ToString();
            txtAmount.Text = invoice.Amount.ToString();
        }

        public string Description => txtDescription.Text;
        public int Quantity => int.TryParse(txtQuantity.Text, out var q) ? q : 0;
        public decimal Rate => decimal.TryParse(txtRate.Text, out var r) ? r : 0m;
        public decimal Amount => decimal.TryParse(txtAmount.Text, out var a) ? a : 0m;

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }
    }
}
