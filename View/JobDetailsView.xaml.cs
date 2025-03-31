using System.Windows;
using System.Windows.Controls;
using CivilProcessERP.Models.Job;
using System.Windows.Input;
using System.Diagnostics;
using CivilProcessERP.Models;

 // Ensure this namespace contains AttachmentModel


namespace CivilProcessERP.Views
{
    public partial class JobDetailsView : UserControl
    {
        public Job Job { get; set; }

        public JobDetailsView(Job job)
        {
            InitializeComponent();
            // If attachments are null, initialize with dummy data for testing
    if (job.Attachments == null || !job.Attachments.Any())
    {
        job.Attachments = new List<AttachmentModel>
        {
            new AttachmentModel
            {
                Purpose = "Invoice",
                Format = "PDF",
                Description = "Sample Invoice File",
                Status = "Ready",
                FilePath = @"C:\Users\GLCS\CivilProcessERP\Assets\sample-invoice.pdf"
            },
            new AttachmentModel
            {
                Purpose = "Picture",
                Format = "JPG",
                Description = "Test Picture",
                Status = "Available",
                FilePath = @"C:\Users\GLCS\CivilProcessERP\Assets\img.png"
            }
        };
    }

    if (job.ChangeHistory == null || !job.ChangeHistory.Any())
{
    job.ChangeHistory = new List<ChangeEntryModel>
    {
        new ChangeEntryModel
        {
            Date = DateTime.Now.AddDays(-3),
            FieldName = "Client",
            OldValue = "ABC Corp",
            NewValue = "XYZ LLC",
            ChangedBy = "Admin"
        },
        new ChangeEntryModel
        {
            Date = DateTime.Now.AddDays(-1),
            FieldName = "Zone",
            OldValue = "North",
            NewValue = "West",
            ChangedBy = "System"
        }
    };
}

            Job = job ?? new Job();
            DataContext = this;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            var mainWindow = Application.Current.MainWindow as MainWindow;
            mainWindow?.RemoveTab(this);
        }

private void AttachmentsListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
{
    if (sender is ListView listView && listView.SelectedItem is AttachmentModel attachment)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = attachment.FilePath, // FilePath or URI
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to open attachment: {ex.Message}");
        }
    }
}
}
}
