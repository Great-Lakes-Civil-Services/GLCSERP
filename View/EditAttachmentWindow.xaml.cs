using System.Windows;
using CivilProcessERP.Models.Job;

namespace CivilProcessERP.Views
{
    public partial class EditAttachmentWindow : Window
    {
        public string Description { get; set; }
        public string Format { get; set; }
        public string Purpose { get; set; }

        public EditAttachmentWindow(AttachmentModel model)
        {
            InitializeComponent();
            // Initialize fields from the model
            Description = model.Description;
            Format = model.Format;
            Purpose = model.Purpose;
            DataContext = this;
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            // Update properties from UI
            Description = DescriptionTextBox.Text.Trim();
            //Format = FormatTextBox.Text.Trim();
            Purpose = PurposeTextBox.Text.Trim();
            DialogResult = true;
            Close();
        }
    }
} 