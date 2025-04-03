using System.Windows;
using System.Windows.Controls;
using static CivilProcessERP.Models.InvoiceEntryModel;
using static CivilProcessERP.Models.PaymentEntryModel;
using CivilProcessERP.Models;
using System.Windows.Input;
using System.Diagnostics;
using CivilProcessERP.Models;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using CivilProcessERP.Models.Job;

// Ensure this namespace contains AttachmentModel


namespace CivilProcessERP.Views
{
    public partial class JobDetailsView : UserControl
    {
        public Job Job { get; set; }

        public JobDetailsView(Job job)
        {
            InitializeComponent();
            LoadMockData();
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

        private void LoadMockData()
    {
        var allLogs = new List<LogEntryModel>
        {
            new LogEntryModel
            {
                Date = new DateTime(2025, 1, 16, 21, 58, 0),
                Body = "Assigned Type of Service: PERSONAL",
                Att = true,
                Aff = false,
                FS = true,
                Source = "Office FLI"
            },
            new LogEntryModel
            {
                Date = new DateTime(2025, 1, 16, 18, 55, 0),
                Body = "Assigned Type of Service: PERSONAL SERVICE",
                Aff = true,
                FS = false,
                Source = "Field Agent"
            }
        };

        AttemptEntries = new ObservableCollection<LogEntryModel>(allLogs.Where(x => x.Att));
        CommentEntries = new ObservableCollection<LogEntryModel>(allLogs.Where(x => !x.Att));
        InvoiceEntries.Add(new InvoiceEntryModel { Description = "Filing Fee", Quantity = 1, Rate = 30 });
InvoiceEntries.Add(new InvoiceEntryModel { Description = "Service Fee", Quantity = 2, Rate = 45 });

PaymentEntries.Add(new PaymentEntryModel { Date = DateTime.Today.AddDays(-2), Method = "Credit", Description = "Initial", Amount = 30 });
PaymentEntries.Add(new PaymentEntryModel { Date = DateTime.Today, Method = "Cash", Description = "Full Pay", Amount = 60 });

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
private void AddAttachment_Click(object sender, RoutedEventArgs e)
{
    var openFileDialog = new OpenFileDialog
    {
        Title = "Select a file to attach",
        Filter = "All Files|*.*",
        Multiselect = false
    };

    if (openFileDialog.ShowDialog() == true)
    {
        string filePath = openFileDialog.FileName;

        var newAttachment = new AttachmentModel
        {
            Purpose = "General", // default or you can prompt
            Format = System.IO.Path.GetExtension(filePath).TrimStart('.').ToUpper(),
            Description = "New Attachment", // optionally prompt user
            Status = "New",
            FilePath = filePath
        };

        Job.Attachments.Add(newAttachment);
        AttachmentsListView.Items.Refresh();
    }
}


private void EditAttachment_Click(object sender, RoutedEventArgs e)
{
    if (AttachmentsListView.SelectedItem is AttachmentModel selected)
    {
        string input = Microsoft.VisualBasic.Interaction.InputBox(
            "Enter new description:", "Edit Attachment Description", selected.Description);

        if (!string.IsNullOrWhiteSpace(input))
        {
            selected.Description = input;
            AttachmentsListView.Items.Refresh();
        }
    }
    else
    {
        MessageBox.Show("Please select an attachment to edit.");
    }
}

private void DeleteAttachment_Click(object sender, RoutedEventArgs e)
{
    if (AttachmentsListView.SelectedItem is AttachmentModel selected)
    {
        Job.Attachments.Remove(selected);
        AttachmentsListView.Items.Refresh();
    }
    else
    {
        MessageBox.Show("Please select an attachment to delete.");
    }
}

private void AddComment_Click(object sender, RoutedEventArgs e)
{
    var newEntry = new LogEntryModel
    {
        Date = DateTime.Now,
        Body = "New Comment",
        Source = "User",
        Att = false // it's a comment
    };

    Job.Comments.Add(newEntry);
}
private void EditComment_Click(object sender, RoutedEventArgs e)
{
    if (CommentsListView.SelectedItem is LogEntryModel selected)
    {
        var editWindow = new EditLogEntryWindow(selected)
        {
            Owner = Window.GetWindow(this)
        };

        bool? result = editWindow.ShowDialog();
        if (result == true)
        {
            CommentsListView.Items.Refresh(); // Reflect changes
        }
    }
    else
    {
        MessageBox.Show("Please select a comment to edit.");
    }
}

private void DeleteComment_Click(object sender, RoutedEventArgs e)
{
    if (CommentsListView.SelectedItem is LogEntryModel selected)
    {
        Job.Comments.Remove(selected);
    }
}

private void AddAttempt_Click(object sender, RoutedEventArgs e)
{
    var newAttempt = new LogEntryModel
    {
        Date = DateTime.Now,
        Body = "New attempt message",
        Aff = true,
        FS = false,
        Source = "System",
        Att = true // so it shows in attempts
    };

    AttemptEntries.Add(newAttempt);
}

private void EditAttempt_Click(object sender, RoutedEventArgs e)
{
    if (AttemptsListView.SelectedItem is LogEntryModel selected)
    {
        var editWindow = new EditLogEntryWindow(selected)
        {
            Owner = Window.GetWindow(this)
        };

        bool? result = editWindow.ShowDialog();
        if (result == true)
        {
            AttemptsListView.Items.Refresh(); // Reflect changes
        }
    }
    else
    {
        MessageBox.Show("Please select an attempt to edit.");
    }
}


private void DeleteAttempt_Click(object sender, RoutedEventArgs e)
{
    if (AttemptsListView.SelectedItem is LogEntryModel selected)
    {
        AttemptEntries.Remove(selected);
    }
}

private void AddInvoice_Click(object sender, RoutedEventArgs e)
{
    var entry = new InvoiceEntryModel();
    var popup = new EditInvoiceWindow(entry) { Owner = Window.GetWindow(this) };
    if (popup.ShowDialog() == true)
    {
        InvoiceEntries.Add(entry);
    }
}

private void EditInvoice_Click(object sender, RoutedEventArgs e)
{
    if (InvoiceListView.SelectedItem is InvoiceEntryModel selected)
    {
        var popup = new EditInvoiceWindow(selected) { Owner = Window.GetWindow(this) };
        popup.ShowDialog();
        InvoiceListView.Items.Refresh();
    }
}

private void DeleteInvoice_Click(object sender, RoutedEventArgs e)
{
    if (InvoiceListView.SelectedItem is InvoiceEntryModel selected)
        InvoiceEntries.Remove(selected);
}

private void AddPayment_Click(object sender, RoutedEventArgs e)
{
    var entry = new PaymentEntryModel{ Date = DateTime.Today };
    var popup = new EditPaymentWindow(entry) { Owner = Window.GetWindow(this) };
    if (popup.ShowDialog() == true)
    {
        PaymentEntries.Add(entry);
    }
}

private void EditPayment_Click(object sender, RoutedEventArgs e)
{
    if (PaymentsListView.SelectedItem is PaymentEntryModel selected)
    {
        var popup = new EditPaymentWindow(selected) { Owner = Window.GetWindow(this) };
        popup.ShowDialog();
        PaymentsListView.Items.Refresh();
    }
}

private void DeletePayment_Click(object sender, RoutedEventArgs e)
{
    if (PaymentsListView.SelectedItem is PaymentEntryModel selected)
        PaymentEntries.Remove(selected);
}



public ObservableCollection<LogEntryModel> AttemptEntries { get; set; }
public ObservableCollection<LogEntryModel> CommentEntries { get; set; }
public ObservableCollection<InvoiceEntryModel> InvoiceEntries { get; set; } = new ObservableCollection<InvoiceEntryModel>();
public ObservableCollection<PaymentEntryModel> PaymentEntries { get; set; } = new ObservableCollection<PaymentEntryModel>();

// private void LoadMockData()
// {
//     var allLogs = new List<LogEntryModel>
//     {
//         new LogEntryModel { Date = new DateTime(2025, 1, 16, 21, 58, 0), Body = "Assigned Type of Service: PERSONAL", Att = true, Source = "Office FLI" },
//         new LogEntryModel { Date = new DateTime(2025, 1, 16, 18, 55, 0), Body = "Assigned Type of Service: PERSONAL SERVICE", Att = false, Source = "Field Agent" },
//         // ... Add more mock entries here
//     };

//     AttemptEntries = new ObservableCollection<LogEntryModel>(allLogs.Where(x => x.Att));
//     CommentEntries = new ObservableCollection<LogEntryModel>(allLogs.Where(x => !x.Att));
// }


}

}
