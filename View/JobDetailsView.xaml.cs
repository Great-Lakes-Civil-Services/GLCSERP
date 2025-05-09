using System.Windows;
using System.Windows.Controls;
using static CivilProcessERP.Models.InvoiceEntryModel;
using static CivilProcessERP.Models.PaymentEntryModel;
using CivilProcessERP.Models;
using System.Windows.Input;
using System.Diagnostics;
using System.ComponentModel;
using CivilProcessERP.Models;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using CivilProcessERP.Models.Job;
using System.IO;
using Npgsql;

// Ensure this namespace contains AttachmentModel


namespace CivilProcessERP.Views
{
    public partial class JobDetailsView : UserControl,INotifyPropertyChanged
    {
       //
        public Job Job { get; set; }

        public JobDetailsView(Job job)
        {
            InitializeComponent();

            
    // Step 1: Fetch court from DB if needed
    if (job != null && !string.IsNullOrEmpty(job.JobId) && string.IsNullOrEmpty(job.Court))
    {
        var jobService = new JobService();
        try
        {
            var dbJob = jobService.GetJobById(job.JobId);
            job.Court = dbJob.Court; // Just court for now
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to load job from database: {ex.Message}");
        }
    }

    // Step 2: Bind to UI
    Job = job ?? new Job();
    DataContext = this;

    // Step 3: Load remaining fields from mock data
    LoadMockData();

            // If attachments are null, initialize with dummy data for testing
    // if (job.Attachments == null || !job.Attachments.Any())
    {
        // job.Attachments = new List<AttachmentModel>
        // {
        //     new AttachmentModel
        //     {
        //         Purpose = "Invoice",
        //         Format = "PDF",
        //         Description = "Sample Invoice File",
        //         Status = "Ready",
        //         FilePath = @"C:\Users\GLCS\CivilProcessERP\Assets\sample-invoice.pdf"
        //     },
        //     new AttachmentModel
        //     {
        //         Purpose = "Picture",
        //         Format = "JPG",
        //         Description = "Test Picture",
        //         Status = "Available",
        //         FilePath = @"C:\Users\GLCS\CivilProcessERP\Assets\img.png"
        //     }
        // };
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
        //InvoiceEntries.Add(new InvoiceEntryModel { Description = "Filing Fee", Quantity = 1, Rate = 30 });
//InvoiceEntries.Add(new InvoiceEntryModel { Description = "Service Fee", Quantity = 2, Rate = 45 });

//PaymentEntries.Add(new PaymentEntryModel { Date = DateTime.Today.AddDays(-2), Method = "Credit", Description = "Initial", Amount = 30 });
//PaymentEntries.Add(new PaymentEntryModel { Date = DateTime.Today, Method = "Cash", Description = "Full Pay", Amount = 60 });

    }

// private void AttachmentsListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
// {
//     if (sender is ListView listView && listView.SelectedItem is AttachmentModel attachment)
//     {
//         try
//         {
//             Process.Start(new ProcessStartInfo
//             {
//                 FileName = attachment.FilePath, // FilePath or URI
//                 UseShellExecute = true
//             });
//         }
//         catch (Exception ex)
//         {
//             MessageBox.Show($"Failed to open attachment: {ex.Message}");
//         }
//     }
// }
private void AttachmentsListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
{
    var selectedAttachment = AttachmentsListView.SelectedItem as CivilProcessERP.Models.Job.AttachmentModel;
    if (selectedAttachment == null)
        return;

    string blobmetadataId = selectedAttachment.BlobMetadataId;
    string fileExtension = selectedAttachment.FileExtension?.Trim().ToLower();

    // 🔍 Debug: Output values before the validation check
    Console.WriteLine($"[DEBUG] Selected BlobMetadataId: {blobmetadataId}");
    Console.WriteLine($"[DEBUG] Selected FileExtension: {fileExtension}");

    if (string.IsNullOrEmpty(blobmetadataId))
    {
        MessageBox.Show("Invalid attachment metadata.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        return;
    }

    byte[] fileData = FetchBlobData(blobmetadataId);

    if (fileData == null || fileData.Length == 0)
    {
        MessageBox.Show("Failed to retrieve file data.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        return;
    }

    try
    {
        string tempFileName = $"attachment_{Guid.NewGuid()}.{fileExtension}";
        string tempFilePath = Path.Combine(Path.GetTempPath(), tempFileName);
        File.WriteAllBytes(tempFilePath, fileData);

        Process.Start(new ProcessStartInfo("chrome.exe", tempFilePath)
{
    UseShellExecute = true
});
    }
    catch (Exception ex)
    {
        MessageBox.Show($"Could not open file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
    }
}

// private byte[] FetchBlobData(string blobmetadataId)
// {
//     byte[] fileData = null;

//     string query = @"
//         SELECT b.blob
//         FROM blobs b
//         JOIN blobmetadata bm ON bm.id = b.id
//         WHERE bm.id = @blobmetadataId::uuid;
//     ";

//     string connectionString = "Host=localhost;Port=5432;Database=mypg_database;Username=postgres;Password=7866";

//     try
//     {
//         using (var conn = new NpgsqlConnection(connectionString))
//         {
//             conn.Open();
//             using (var cmd = new NpgsqlCommand(query, conn))
//             {
//                 cmd.Parameters.AddWithValue("blobmetadataId", Guid.Parse(blobmetadataId)); // Use Guid.Parse for UUID
//                 fileData = cmd.ExecuteScalar() as byte[];
//             }
//         }
//     }
//     catch (Exception ex)
//     {
//         MessageBox.Show($"Database error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
//     }

//     return fileData;
// }
private byte[] FetchBlobData(string blobmetadataId)
{
    byte[] fileData = null;

    string query = @"
        SELECT b.blob
        FROM blobmetadata bm
        JOIN blobs b ON bm.id = b.id
        WHERE bm.id = @blobmetadataId::uuid;
    ";

    string connectionString = "Host=localhost;Port=5432;Database=mypg_database;Username=postgres;Password=7866";

    using (var conn = new NpgsqlConnection(connectionString))
    {
        conn.Open();
        using (var cmd = new NpgsqlCommand(query, conn))
        {
            cmd.Parameters.AddWithValue("blobmetadataId", Guid.Parse(blobmetadataId));
            fileData = cmd.ExecuteScalar() as byte[];  // Retrieves the blob data as a byte array
        }
    }

    return fileData;
}



private void AddAttachment_Click(object sender, RoutedEventArgs e)
{
    // var openFileDialog = new OpenFileDialog
    // {
    //     Title = "Select a file to attach",
    //     Filter = "All Files|*.*",
    //     Multiselect = false
    // };

    // if (openFileDialog.ShowDialog() == true)
    // {
    //     string filePath = openFileDialog.FileName;

    //     var newAttachment = new AttachmentModel
    //     {
    //         Purpose = "General", // default or you can prompt
    //         Format = System.IO.Path.GetExtension(filePath).TrimStart('.').ToUpper(),
    //         Description = "New Attachment", // optionally prompt user
    //         Status = "New",
    //         FilePath = filePath
    //     };

    //     Job.Attachments.Add(newAttachment);
    //     AttachmentsListView.Items.Refresh();
    // }
}

private void EditAttachment_Click(object sender, RoutedEventArgs e)
{
    if (AttemptsListView.SelectedItem is LogEntryModel selected)
    {
        var editWindow = new EditLogEntryWindow(selected) { Owner = Window.GetWindow(this) };

        if (editWindow.ShowDialog() == true)
        {
            selected.Date = editWindow.SelectedDate.Date.Add(editWindow.SelectedTime);  // ✅ fixed

            selected.Body = editWindow.Body;
            selected.Aff = editWindow.Aff;
            selected.FS = editWindow.DS;   // ✅ be sure your dialog exposes property as Ds or FS
            selected.Att = editWindow.Att;
            selected.Source = editWindow.Source;

            AttemptsListView.Items.Refresh();
        }
    }
    else
    {
        MessageBox.Show("Please select an attempt to edit.");
    }
}

private void DeleteAttachment_Click(object sender, RoutedEventArgs e)
{
    // if (AttachmentsListView.SelectedItem is AttachmentModel selected)
    // {
    //     Job.Attachments.Remove(selected);
    //     AttachmentsListView.Items.Refresh();
    // }
    // else
    // {
    //     MessageBox.Show("Please select an attachment to delete.");
    // }
    }
// }

private void AddComment_Click(object sender, RoutedEventArgs e)
{
    var newEntry = new LogEntryModel
    {
        Date = DateTime.Now,
        Body = "New Comment",
        Source = "User",
        Att = false // it's a comment
    };

    //Job.Comments.Add(newEntry);
}
private void EditComment_Click(object sender, RoutedEventArgs e)
{
    if (CommentsListView.SelectedItem is CommentModel selectedComment)
    {
        // map CommentModel to LogEntryModel
        var tempLogEntry = new LogEntryModel
        {
            Date = DateTime.Parse($"{selectedComment.Date} {selectedComment.Time}"),
            Body = selectedComment.Body,
            Aff = selectedComment.Aff,
            FS = selectedComment.FS,
            Att = selectedComment.Att,
            Source = selectedComment.Source
        };

        var editWindow = new EditLogEntryWindow(tempLogEntry) { Owner = Window.GetWindow(this) };

        if (editWindow.ShowDialog() == true)
        {
            // update CommentModel back from LogEntryModel
            selectedComment.Date = editWindow.SelectedDate.ToString("yyyy-MM-dd");
            selectedComment.Time = editWindow.SelectedDate.ToString("HH:mm:ss");  // or editWindow.SelectedTime if separate
            selectedComment.Body = editWindow.Body;
            selectedComment.Aff = editWindow.Aff;
            selectedComment.FS = editWindow.DS;
            selectedComment.Att = editWindow.Att;
            selectedComment.Source = editWindow.Source;

            CommentsListView.Items.Refresh();
        }
    }
    else
    {
        MessageBox.Show("Please select a comment to edit.");
    }
}


private void DeleteComment_Click(object sender, RoutedEventArgs e)
{
//     if (CommentsListView.SelectedItem is LogEntryModel selected)
//     {
//         Job.Comments.Remove(selected);
//     }
// }
}

private void AddAttempt_Click(object sender, RoutedEventArgs e)
{
    // var newAttempt = new LogEntryModel
    // {
    //     Date = DateTime.Now,
    //     Body = "New attempt message",
    //     Aff = true,
    //     FS = false,
    //     Source = "System",
    //     Att = true // so it shows in attempts
    // };

    // AttemptEntries.Add(newAttempt);
}

    private void EditAttempt_Click(object sender, RoutedEventArgs e)
{
    if (AttemptsListView.SelectedItem is AttemptsModel selectedAttempt)
    {
        var tempLogEntry = new LogEntryModel
        {
            Date = DateTime.Parse($"{selectedAttempt.Date} {selectedAttempt.Time}"),
            Body = selectedAttempt.Body,
            Aff = selectedAttempt.Aff,
            FS = selectedAttempt.FS,
            Att = selectedAttempt.Att,
            Source = selectedAttempt.Source
        };

        var editWindow = new EditLogEntryWindow(tempLogEntry) { Owner = Window.GetWindow(this) };

        if (editWindow.ShowDialog() == true)
        {
            selectedAttempt.Date = editWindow.SelectedDate.ToString("yyyy-MM-dd");
            selectedAttempt.Time = editWindow.SelectedDate.ToString("HH:mm:ss");
            selectedAttempt.Body = editWindow.Body;
            selectedAttempt.Aff = editWindow.Aff;
            selectedAttempt.FS = editWindow.DS;
            selectedAttempt.Att = editWindow.Att;
            selectedAttempt.Source = editWindow.Source;

            AttemptsListView.Items.Refresh();
        }
    }
    else
    {
        MessageBox.Show("Please select an attempt to edit.");
    }
}



private void DeleteAttempt_Click(object sender, RoutedEventArgs e)
{
    // if (AttemptsListView.SelectedItem is LogEntryModel selected)
    // {
    //     AttemptEntries.Remove(selected);
    // }
}

private void AddInvoice_Click(object sender, RoutedEventArgs e)
{
    var entry = new InvoiceModel();
var popup = new EditInvoiceWindow(entry) { Owner = Window.GetWindow(this) };

    // if (popup.ShowDialog() == true)
    // {
    //     InvoiceEntries.Add(entry);z
    //      RecalculateTotals();
    // }
}

private void EditInvoice_Click(object sender, RoutedEventArgs e)
{
    if (InvoiceListView.SelectedItem is InvoiceModel selected)
    {
        var editWindow = new EditInvoiceWindow(selected);
        editWindow.Owner = Window.GetWindow(this);
        if (editWindow.ShowDialog() == true)
        {
            selected.Description = editWindow.Description;
            selected.Quantity = editWindow.Quantity;
            selected.Rate = editWindow.Rate;
            selected.Amount = editWindow.Amount;

            InvoiceListView.Items.Refresh();
            OnPropertyChanged(nameof(Job.TotalInvoiceAmount));
        }
    }
    else
    {
        MessageBox.Show("Please select an invoice to edit.");
    }
}


private void DeleteInvoice_Click(object sender, RoutedEventArgs e)
{
    // if (InvoiceListView.SelectedItem is InvoiceEntryModel selected)
    //     InvoiceEntries.Remove(selected);
    //      RecalculateTotals();
}

private void AddPayment_Click(object sender, RoutedEventArgs e)
{
   var entry = new PaymentModel();
var popup = new EditPaymentWindow(entry) { Owner = Window.GetWindow(this) };

    if (popup.ShowDialog() == true)
    {
        // PaymentEntries.Add(entry);
        //  RecalculateTotals();
    }
}

private void EditPayment_Click(object sender, RoutedEventArgs e)
{
    if (PaymentsListView.SelectedItem is PaymentModel selected)
    {
        var editWindow = new EditPaymentWindow(selected) { Owner = Window.GetWindow(this) };
        if (editWindow.ShowDialog() == true)
        {
            if (DateTime.TryParseExact(editWindow.Date + " " + editWindow.Time, "yyyy-MM-dd HH:mm:ss", null, System.Globalization.DateTimeStyles.None, out var parsedDateTime))
            {
                selected.Date = parsedDateTime;
                selected.TimeOnly = editWindow.Time;
            }
            else
            {
                MessageBox.Show("Invalid date/time format.");
                return;
            }

            selected.Method = editWindow.Method;
            selected.Description = editWindow.Description;
            selected.Amount = editWindow.Amount;

            PaymentsListView.Items.Refresh();
            OnPropertyChanged(nameof(Job.TotalPaymentsAmount));
        }
    }
    else
    {
        MessageBox.Show("Please select a payment to edit.");
    }
}

private void DeletePayment_Click(object sender, RoutedEventArgs e)
{
    // if (PaymentsListView.SelectedItem is PaymentEntryModel selected)
    //     PaymentEntries.Remove(selected);
    //      RecalculateTotals();
}

public void RecalculateTotals()
{
    //OnPropertyChanged(nameof(TotalInvoiceAmount));
   // OnPropertyChanged(nameof(TotalPaymentsAmount));
}



public event PropertyChangedEventHandler PropertyChanged;
protected void OnPropertyChanged(string name)
{
    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

private void AddInvoices_Click(object sender, RoutedEventArgs e)
{
    var entry = new CivilProcessERP.Models.Job.InvoiceModel();
    var popup = new EditInvoiceWindow(entry) { Owner = Window.GetWindow(this) };
    if (popup.ShowDialog() == true)
    {
        //InvoiceEntries.Add(entry);
        RecalculateTotals(); // 👈 Trigger UI update
    }
}
private void EditCourt_MouseDoubleClick(object sender, MouseButtonEventArgs e)
{
    var dialog = new EditFieldDialog("Court", Job.Court, ""); // Pass second param as empty
    if (dialog.ShowDialog() == true)
    {
        Job.Court = dialog.FirstName; // Just use the FirstName field for single-value
        OnPropertyChanged(nameof(Job.Court));
    }
}

private void EditDefendant_MouseDoubleClick(object sender, MouseButtonEventArgs e)
{
    var parts = (Job.Defendant ?? "").Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
    string firstName = parts.Length > 0 ? parts[0] : "";
    string lastName = parts.Length > 1 ? parts[1] : "";

    var dialog = new EditFieldDialog("Defendant", firstName, lastName);
    if (dialog.ShowDialog() == true)
    {
        Job.Defendant = $"{dialog.FirstName} {dialog.LastName}".Trim();
        OnPropertyChanged(nameof(Job.Defendant));
    }
}

private void EditPlaintiff_MouseDoubleClick(object sender, MouseButtonEventArgs e)
{
    var parts = (Job.Plaintiff ?? "").Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
    string firstName = parts.Length > 0 ? parts[0] : "";
    string lastName = parts.Length > 1 ? parts[1] : "";

    var dialog = new EditFieldDialog("Plaintiff", firstName, lastName);
    if (dialog.ShowDialog() == true)
    {
        Job.Plaintiff = $"{dialog.FirstName} {dialog.LastName}".Trim();
        OnPropertyChanged(nameof(Job.Plaintiff));
    }
}

private void EditAttorney_MouseDoubleClick(object sender, MouseButtonEventArgs e)
{
    var parts = (Job.Attorney ?? "").Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
    string firstName = parts.Length > 0 ? parts[0] : "";
    string lastName = parts.Length > 1 ? parts[1] : "";

    var dialog = new EditFieldDialog("Attorney", firstName, lastName);
    if (dialog.ShowDialog() == true)
    {
        Job.Attorney = $"{dialog.FirstName} {dialog.LastName}".Trim();
        OnPropertyChanged(nameof(Job.Attorney));
    }
}

private void EditProcessServer_MouseDoubleClick(object sender, MouseButtonEventArgs e)
{
    var parts = (Job.ProcessServer ?? "").Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
    string firstName = parts.Length > 0 ? parts[0] : "";
    string lastName = parts.Length > 1 ? parts[1] : "";

    var dialog = new EditFieldDialog("Process Server", firstName, lastName);
    if (dialog.ShowDialog() == true)
    {
        Job.ProcessServer = $"{dialog.FirstName} {dialog.LastName}".Trim();
        OnPropertyChanged(nameof(Job.ProcessServer));
    }
}

private void EditClient_MouseDoubleClick(object sender, MouseButtonEventArgs e)
{
    var parts = (Job.Client ?? "").Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
    string firstName = parts.Length > 0 ? parts[0] : "";
    string lastName = parts.Length > 1 ? parts[1] : "";

    var dialog = new EditFieldDialog("Client", firstName, lastName);
    if (dialog.ShowDialog() == true)
    {
        Job.Client = $"{dialog.FirstName} {dialog.LastName}".Trim();
        OnPropertyChanged(nameof(Job.Client));
    }
}

private void EditSQLDateTimeCreated_MouseDoubleClick(object sender, MouseButtonEventArgs e)
{
    var sqlDateTimeCreated = DateTime.TryParse(Job.SqlDateTimeCreated, out var parsedDate) ? (DateTime?)parsedDate : null;
    var dialog = new EditDateDialog("SQL DateTime Created", sqlDateTimeCreated);
    if (dialog.ShowDialog() == true && dialog.SelectedDate != null)
    {
        Job.SqlDateTimeCreated = dialog.SelectedDate.Value.ToString("yyyy-MM-dd HH:mm:ss");
        OnPropertyChanged(nameof(Job.SqlDateTimeCreated));
    }
}

private void EditClientStatus_MouseDoubleClick(object sender, MouseButtonEventArgs e)
{
    var dialog = new SingleFieldDialog("Client Status", Job.ClientStatus);
    if (dialog.ShowDialog() == true)
    {
        Job.ClientStatus = dialog.Value;
        OnPropertyChanged(nameof(Job.ClientStatus));
    }
}

private void EditArea_MouseDoubleClick(object sender, MouseButtonEventArgs e)
{
    var dialog = new SingleFieldDialog("Area", Job.Zone);
    if (dialog.ShowDialog() == true)
    {
        Job.Zone = dialog.Value;
        OnPropertyChanged(nameof(Job.Zone));
    }
}

private void EditTypeOfWrit_MouseDoubleClick(object sender, MouseButtonEventArgs e)
{
    var dialog = new SingleFieldDialog("Type of Writ", Job.TypeOfWrit);
    if (dialog.ShowDialog() == true)
    {
        Job.TypeOfWrit = dialog.Value;
        OnPropertyChanged(nameof(Job.TypeOfWrit));
    }
}

private void EditServiceType_MouseDoubleClick(object sender, MouseButtonEventArgs e)
{
    var dialog = new SingleFieldDialog("Service Info", Job.ServiceType);
    if (dialog.ShowDialog() == true)
    {
        Job.ServiceType = dialog.Value;
        OnPropertyChanged(nameof(Job.ServiceType));
    }
}

private void EditTime_MouseDoubleClick(object sender, MouseButtonEventArgs e)
{
    var dialog = new EditTimeDialog("Court Time", TimeSpan.TryParse(Job.Time, out var timeVal) ? timeVal : (TimeSpan?)null);
    if (dialog.ShowDialog() == true && dialog.SelectedTime.HasValue)
    {
        Job.Time = dialog.SelectedTime.Value.ToString(@"hh\:mm");
        OnPropertyChanged(nameof(Job.Time));
    }
}

private void EditServiceTime_MouseDoubleClick(object sender, MouseButtonEventArgs e)
{
    var dialog = new EditTimeDialog("Service Time", TimeSpan.TryParse(Job.ServiceTime, out var timeVal) ? timeVal : (TimeSpan?)null);
    if (dialog.ShowDialog() == true && dialog.SelectedTime.HasValue)
    {
        Job.ServiceTime = dialog.SelectedTime.Value.ToString(@"hh\:mm");
        OnPropertyChanged(nameof(Job.ServiceTime));
    }
}

private void EditJobDate_MouseDoubleClick(object sender, MouseButtonEventArgs e)
{
    var dialog = new EditDateDialog("Court Date", DateTime.TryParse(Job.Date, out var dateVal) ? dateVal : (DateTime?)null);
    if (dialog.ShowDialog() == true && dialog.SelectedDate.HasValue)
    {
        Job.Date = dialog.SelectedDate.Value.ToString("MM/dd/yyyy");  // Format as needed
        OnPropertyChanged(nameof(Job.Date));
    }
}

private void EditServiceDate_MouseDoubleClick(object sender, MouseButtonEventArgs e)
{
    var dialog = new EditDateDialog("Service Date", DateTime.TryParse(Job.ServiceDate, out var dateVal) ? dateVal : (DateTime?)null);
    if (dialog.ShowDialog() == true && dialog.SelectedDate.HasValue)
    {
        Job.ServiceDate = dialog.SelectedDate.Value.ToString("MM/dd/yyyy");  // Format as needed
        OnPropertyChanged(nameof(Job.ServiceDate));
    }
}

private void EditSqlDateTimeCreated_MouseDoubleClick(object sender, MouseButtonEventArgs e)
{
    var existing = DateTime.TryParse(Job.SqlDateTimeCreated, out var parsed) ? parsed : (DateTime?)null;
    var dialog = new EditDateTimeDialog("SQL DateTime Created", existing);
    if (dialog.ShowDialog() == true && dialog.SelectedDateTime.HasValue)
    {
        Job.SqlDateTimeCreated = dialog.SelectedDateTime.Value.ToString("MM/dd/yyyy HH:mm");
        OnPropertyChanged(nameof(Job.SqlDateTimeCreated));
    }
}

private void EditExpirationDate_MouseDoubleClick(object sender, MouseButtonEventArgs e)
{
    var existing = DateTime.TryParse(Job.ExpirationDate, out var parsed) ? parsed : (DateTime?)null;
    var dialog = new EditDateTimeDialog("Expiration Date", existing);
    if (dialog.ShowDialog() == true && dialog.SelectedDateTime.HasValue)
    {
        Job.ExpirationDate = dialog.SelectedDateTime.Value.ToString("MM/dd/yyyy HH:mm");
        OnPropertyChanged(nameof(Job.ExpirationDate));
    }
}

private void EditLastDayToServe_MouseDoubleClick(object sender, MouseButtonEventArgs e)
{
    var existing = DateTime.TryParse(Job.LastDayToServe, out var parsed) ? parsed : (DateTime?)null;
    var dialog = new EditDateTimeDialog("Last Day to Serve Date", existing);
    if (dialog.ShowDialog() == true && dialog.SelectedDateTime.HasValue)
    {
        Job.LastDayToServe = dialog.SelectedDateTime.Value.ToString("MM/dd/yyyy HH:mm");
        OnPropertyChanged(nameof(Job.LastDayToServe));
    }
}


public ObservableCollection<LogEntryModel> AttemptEntries { get; set; }
public ObservableCollection<LogEntryModel> CommentEntries { get; set; }
//public ObservableCollection<InvoiceEntryModel> InvoiceEntries { get; set; } = new ObservableCollection<InvoiceEntryModel>();
//public ObservableCollection<PaymentEntryModel> PaymentEntries { get; set; } = new ObservableCollection<PaymentEntryModel>();

//public decimal TotalInvoiceAmount => InvoiceEntries?.Sum(x => x.Amount) ?? 0;
//public decimal TotalPaymentsAmount => PaymentEntries?.Sum(x => x.Amount) ?? 0;


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
