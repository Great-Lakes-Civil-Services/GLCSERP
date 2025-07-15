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
using CivilProcessERP.ViewModels;
using Npgsql;
using CivilProcessERP.Services;

// Ensure this namespace contains AttachmentModel


namespace CivilProcessERP.Views
{
    public partial class JobDetailsView : System.Windows.Controls.UserControl, INotifyPropertyChanged
    {
        private readonly SemaphoreSlim _blobLock = new SemaphoreSlim(1, 1); // 🔒 Thread-safe guard for blob preview
        private readonly SemaphoreSlim _fileAccessLock = new SemaphoreSlim(1, 1); // 🔒 Thread-safe guard for file access

        private string _loggedInRoleName;
        private Job _originalJob;
        public Job Job { get; set; }

        private int _jobCountForCaseNumber;
        public int JobCountForCaseNumber
        {
            get => _jobCountForCaseNumber;
            set
            {
                _jobCountForCaseNumber = value;
                OnPropertyChanged(nameof(JobCountForCaseNumber));
            }
        }

        public JobDetailsView(Job job)
{
             InitializeComponent();
            DataContext = new LandlordTenantViewModel(job);

    // Initialize job object
            Job = job ?? new Job();
    _originalJob = Job.Clone();
    DataContext = this;

    Loaded += JobDetailsView_Loaded; // ✅ async logic will run after control is loaded
}

      private async void JobDetailsView_Loaded(object sender, RoutedEventArgs e)
{
    try
    {
        var roleService = new RolePermissionService();
        _loggedInRoleName = await roleService.GetRoleNameByIdAsync(SessionManager.CurrentUser.RoleNumber);

        DisableFinancialControlsIfUnauthorized();

        if (!string.IsNullOrEmpty(Job.JobId) && string.IsNullOrEmpty(Job.Court))
        {
            await FetchCourtFromDatabaseAsync(Job.JobId);
        }

        EnsureChangeHistoryExists(Job);

        if (!string.IsNullOrEmpty(Job.CaseNumber))
        {
            var jobService = new JobService();
            JobCountForCaseNumber = await jobService.GetJobCountByCaseNumberAsync(Job.CaseNumber);
        }
    }
    catch (Exception ex)
    {
        System.Windows.MessageBox.Show($"Initialization failed: {ex.Message}", "Error", MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
    }
}



        private async Task FetchCourtFromDatabaseAsync(string jobId)
        {
            try
            {
                var jobService = new JobService();
                var dbJob = await Task.Run(() => jobService.GetJobById(jobId));
                if (!string.IsNullOrEmpty(dbJob?.Court))
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        Job.Court = dbJob.Court;
                        OnPropertyChanged(nameof(Job));
                    });
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Failed to load job from DB: {ex.Message}", "Database Error", MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private void EnsureChangeHistoryExists(Job job)
        {
            // if (job.ChangeHistory == null || !job.ChangeHistory.Any())
            // {
            //     job.ChangeHistory = new List<ChangeEntryModel>
            // {
            //     new ChangeEntryModel
            //     {
            //         Date = DateTime.Now.AddDays(-3),
            //         FieldName = "Client",
            //         OldValue = "ABC Corp",
            //         NewValue = "XYZ LLC",
            //         ChangedBy = "Admin"
            //     },
            //     new ChangeEntryModel
            //     {
            //         Date = DateTime.Now.AddDays(-1),
            //         FieldName = "Zone",
            //         OldValue = "North",
            //         NewValue = "West",
            //         ChangedBy = "System"
            //     }
            // };
            // }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            var mainWindow = Window.GetWindow(this) as MainWindow;
            if (mainWindow != null)
                mainWindow.RemoveTab(this);
        }

        private void DisableFinancialControlsIfUnauthorized()
        {
            bool isAuthorized = _loggedInRoleName == "Admin" || _loggedInRoleName == "LT Manager";

            AddInvoiceButton.IsEnabled = isAuthorized;
            EditInvoiceButton.IsEnabled = isAuthorized;
            DeleteInvoiceButton.IsEnabled = isAuthorized;

            AddPaymentButton.IsEnabled = isAuthorized;
            EditPaymentButton.IsEnabled = isAuthorized;
            DeletePaymentButton.IsEnabled = isAuthorized;
        }



   // ⚙️ Step 1: Load mock data (used in design-time or fallback)
private async Task LoadMockDataAsync()
{
    Console.WriteLine("🔧 Loading mock data into AttemptEntries and CommentEntries...");

    await Task.Delay(50); // Optional: Simulate async for uniformity

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
            Att = true,
            Aff = true,
            FS = false,
            Source = "Field Agent"
        }
    };

    AttemptEntries = new ObservableCollection<LogEntryModel>(allLogs.Where(x => x.Att));
    CommentEntries = new ObservableCollection<LogEntryModel>(allLogs.Where(x => !x.Att));

    Console.WriteLine($"✅ Loaded {AttemptEntries.Count} attempts and {CommentEntries.Count} comments.");
}
// ⚙️ Step 2: Double-click file preview handler for attachments (Async-safe)
private async void AttachmentsListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
{
    var selectedAttachment = AttachmentsListView.SelectedItem as CivilProcessERP.Models.Job.AttachmentModel;
    if (selectedAttachment == null)
    {
        Console.WriteLine("⚠️ No attachment selected.");
        return;
    }

    string blobmetadataId = selectedAttachment.BlobMetadataId;
    string fileExtension = selectedAttachment.FileExtension?.Trim().ToLower();

    Console.WriteLine($"🔍 [DEBUG] BlobMetadataId: {blobmetadataId}");
    Console.WriteLine($"🔍 [DEBUG] FileExtension: {fileExtension}");

    if (string.IsNullOrEmpty(blobmetadataId))
    {
        System.Windows.MessageBox.Show("Invalid attachment metadata.", "Error", MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        Console.WriteLine("❌ Attachment missing BlobMetadataId.");
        return;
    }

    await _blobLock.WaitAsync(); // 🔒 Lock access to shared blob preview
    try
    {
        byte[] fileData = await FetchBlobDataAsync(blobmetadataId); // 📥 Fetch asynchronously

        if (fileData == null || fileData.Length == 0)
        {
            System.Windows.MessageBox.Show("Failed to retrieve file data.", "Error", MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            Console.WriteLine("❌ No file data retrieved.");
            return;
        }

        string tempFileName = $"attachment_{Guid.NewGuid()}.{fileExtension}";
        string tempFilePath = Path.Combine(Path.GetTempPath(), tempFileName);
        await File.WriteAllBytesAsync(tempFilePath, fileData);
        Console.WriteLine($"📄 Temporary file written: {tempFilePath}");

        Process.Start(new ProcessStartInfo("chrome.exe", tempFilePath)
        {
            UseShellExecute = true
        });

        Console.WriteLine("🚀 Opened file in Chrome.");
    }
    catch (Exception ex)
    {
        System.Windows.MessageBox.Show($"Could not open file: {ex.Message}", "Error", MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        Console.WriteLine($"🔥 File open error: {ex.Message}");
    }
    finally
    {
        _blobLock.Release(); // 🔓 Unlock
    }
}

private async Task<byte[]> FetchBlobDataAsync(string blobmetadataId)
{
    byte[] fileData = null;

    string query = @"
        SELECT b.blob
        FROM blobmetadata bm
        JOIN blobs b ON bm.id = b.id
        WHERE bm.id = @blobmetadataId::uuid;
    ";

    string connectionString = "Host=localhost;Port=5432;Database=mypg_database;Username=postgres;Password=7866";

    await _fileAccessLock.WaitAsync();
    try
    {
        await using var conn2 = new NpgsqlConnection(connectionString);
        await conn2.OpenAsync();

        await using var cmd = new NpgsqlCommand(query, conn2);
        cmd.Parameters.AddWithValue("blobmetadataId", Guid.Parse(blobmetadataId));

        var result = await cmd.ExecuteScalarAsync();
        fileData = result as byte[];
    }
    catch (Exception ex)
    {
        Console.WriteLine($"🔥 ERROR fetching blob: {ex.Message}");
    }
    finally
    {
        _fileAccessLock.Release();
    }

    return fileData ?? Array.Empty<byte>();
}

private async void AddAttachment_Click(object sender, RoutedEventArgs e)
{
    var dialog = new Microsoft.Win32.OpenFileDialog
    {
        Title = "Select a file to attach",
        Filter = "All Files|*.*",
        Multiselect = false
    };

    if (dialog.ShowDialog() == true)
    {
        var filePath = dialog.FileName;
        var fileExtension = Path.GetExtension(filePath).TrimStart('.').ToUpper();
        var newBlobId = Guid.NewGuid();
        var newAttachmentId = Guid.NewGuid();

        byte[] fileData;
        try
        {
            fileData = await File.ReadAllBytesAsync(filePath);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Failed to read file: {ex.Message}", "Error", MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            return;
        }

        var model = new AttachmentModel
        {
            Id = newAttachmentId,
            BlobId = newBlobId,
            FilePath = filePath,
            FileData = fileData,
            Format = fileExtension,
            Purpose = "General",
            Description = Path.GetFileName(filePath),
            Status = "New"
        };

        Job.Attachments.Add(model);
        AttachmentsListView.Items.Refresh();
        Console.WriteLine("➕ Attachment added.");
    }
}

        private async void EditAttachment_Click(object sender, RoutedEventArgs e)
        {
            if (AttachmentsListView.SelectedItem is AttachmentModel selected)
            {
                var dialog = new Microsoft.Win32.OpenFileDialog
                {
                    Title = "Replace with new file",
                    Filter = "All Files|*.*"
                };

                if (dialog.ShowDialog() == true)
                {
                    string newPath = dialog.FileName;
                    byte[] newData;
                    try
                    {
                        newData = await File.ReadAllBytesAsync(newPath);
                    }
                    catch (Exception ex)
                    {
                        System.Windows.MessageBox.Show($"Could not read new file: {ex.Message}", "Error", MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                        return;
                    }

                    string newExtension = Path.GetExtension(newPath).TrimStart('.').ToUpper();

                    selected.FilePath = newPath;
                    selected.FileData = newData;
                    selected.Format = newExtension;
                    selected.Description = Path.GetFileName(newPath);
                    selected.Status = "Edited";

                    AttachmentsListView.Items.Refresh();
                    Console.WriteLine($"✏️ Attachment updated: {selected.Description}");
                }
            }
        }
private readonly SemaphoreSlim _attachmentLock = new SemaphoreSlim(1, 1);
private readonly SemaphoreSlim _commentLock = new SemaphoreSlim(1, 1);


private async void DeleteAttachment_Click(object sender, RoutedEventArgs e)
{
    if (AttachmentsListView.SelectedItem is not AttachmentModel selected)
    {
        Console.WriteLine("⚠️ No attachment selected for deletion.");
        return;
    }

    await _attachmentLock.WaitAsync();
    try
    {
        if (selected.Id != Guid.Empty)
        {
            Job.DeletedAttachmentId = selected.Id;
            Console.WriteLine($"🗑️ Marked for DB delete: {selected.Id}");
        }

        Job.Attachments.Remove(selected);
        AttachmentsListView.Items.Refresh();
        Console.WriteLine("✅ Attachment removed from UI list.");
    }
    finally
    {
        _attachmentLock.Release();
    }
}
private async void AddComment_Click(object sender, RoutedEventArgs e)
{
    await _commentLock.WaitAsync();
    try
    {
        var newEntry = new CommentModel
        {
            Date = DateTime.Now.ToString("yyyy-MM-dd"),
            Time = DateTime.Now.ToString("HH:mm:ss"),
            Body = "New Comment",
            Source = "User",
            Aff = false,
            FS = false,
            Att = false
        };

        Job.Comments.Add(newEntry);
        CommentsListView.Items.Refresh();
        Console.WriteLine("➕ New comment added.");
    }
    finally
    {
        _commentLock.Release();
    }
}

private async void EditComment_Click(object sender, RoutedEventArgs e)
{
    if (CommentsListView.SelectedItem is not CommentModel selected)
    {
        System.Windows.MessageBox.Show("Please select a comment to edit.", "No Selection", MessageBoxButton.OK, MessageBoxImage.Warning);
        return;
    }

    await _commentLock.WaitAsync();
    try
    {
        var temp = new LogEntryModel
        {
            Date = DateTime.Parse($"{selected.Date} {selected.Time}"),
            Body = selected.Body,
            Aff = selected.Aff,
            FS = selected.FS,
            Att = selected.Att,
            Source = selected.Source
        };

        var win = new EditLogEntryWindow(temp) { Owner = Window.GetWindow(this) };

        if (win.ShowDialog() == true)
        {
            selected.Date = win.SelectedDate.ToString("yyyy-MM-dd");
            selected.Time = win.SelectedDate.ToString("HH:mm:ss");
            selected.Body = win.Body;
            selected.Aff = win.Aff;
            selected.FS = win.DS;
            selected.Att = win.Att;
            selected.Source = win.Source;

            CommentsListView.Items.Refresh();
            Console.WriteLine($"✏️ Comment updated: {selected.Body}");
        }
    }
    finally
    {
        _commentLock.Release();
    }
}
private void DeleteComment_Click(object sender, RoutedEventArgs e)
{
    if (CommentsListView.SelectedItem is CommentModel selected)
    {
        Job.Comments.Remove(selected);
        CommentsListView.Items.Refresh();
    }
}


private void AddAttempt_Click(object sender, RoutedEventArgs e)
{
    var newAttempt = new AttemptsModel
    {
        Date = DateTime.Now.ToString("yyyy-MM-dd"),
        Time = DateTime.Now.ToString("HH:mm:ss"),
        Body = "New attempt",
        Source = "UI",
        Aff = false,
        FS = false,
        Att = true
    };

    Job.Attempts.Add(newAttempt);
    AttemptsListView.Items.Refresh();
}


  private void EditAttempt_Click(object sender, RoutedEventArgs e)
{
    if (AttemptsListView.SelectedItem is AttemptsModel selectedAttempt)
    {
        var temp = new LogEntryModel
        {
            Date = DateTime.Parse($"{selectedAttempt.Date} {selectedAttempt.Time}"),
            Body = selectedAttempt.Body,
            Aff = selectedAttempt.Aff,
            FS = selectedAttempt.FS,
            Att = selectedAttempt.Att,
            Source = selectedAttempt.Source
        };

        var editWindow = new EditLogEntryWindow(temp) { Owner = Window.GetWindow(this) };

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
        System.Windows.MessageBox.Show("Please select an attempt to edit.");
    }
}

private readonly SemaphoreSlim _invoiceLock = new SemaphoreSlim(1, 1);
private readonly SemaphoreSlim _attemptDeleteLock = new SemaphoreSlim(1, 1);


private async void DeleteAttempt_Click(object sender, RoutedEventArgs e)
{
    if (AttemptsListView.SelectedItem is not AttemptsModel selectedAttempt)
        return;

    await _attemptDeleteLock.WaitAsync();
    try
    {
        Job.Attempts.Remove(selectedAttempt);
        AttemptsListView.Items.Refresh();
        Console.WriteLine("🗑️ Attempt deleted.");
    }
    finally
    {
        _attemptDeleteLock.Release();
    }
}


private async void AddInvoice_Click(object sender, RoutedEventArgs e)
{
    await _invoiceLock.WaitAsync();
    try
    {
        var entry = new InvoiceModel
        {
            Id = Guid.NewGuid(),
            Description = "New Invoice",
            Quantity = 1,
            Rate = 0m,
            Amount = 0m
        };
        Job.InvoiceEntries.Add(entry);
        OnPropertyChanged(nameof(Job.TotalInvoiceAmount));
        Console.WriteLine("➕ Dummy invoice row added.");
        // Optionally select the new row in the UI:
        InvoiceListView.SelectedItem = entry;
    }
    finally
    {
        _invoiceLock.Release();
    }
}


private async void EditInvoice_Click(object sender, RoutedEventArgs e)
{
    if (InvoiceListView.SelectedItem is not InvoiceModel selected)
    {
        System.Windows.MessageBox.Show("Please select an invoice to edit.");
        return;
    }

    await _invoiceLock.WaitAsync();
    try
    {
        var editWindow = new EditInvoiceWindow(selected)
        {
            Owner = Window.GetWindow(this)
        };

        if (editWindow.ShowDialog() == true)
        {
            selected.Description = editWindow.Description;
            selected.Quantity = editWindow.Quantity;
            selected.Rate = editWindow.Rate;
            selected.Amount = editWindow.Amount;

            InvoiceListView.Items.Refresh();
            OnPropertyChanged(nameof(Job.TotalInvoiceAmount));
            Console.WriteLine("✏️ Invoice edited.");
        }
    }
    finally
    {
        _invoiceLock.Release();
    }
}

// private async void DeleteInvoice_Click(object sender, RoutedEventArgs e)
// {
//     if (InvoiceListView.SelectedItem is not InvoiceModel selected)
//         return;

//     await _invoiceLock.WaitAsync();
//     try
//     {
//         if (selected.Id != Guid.Empty)
//         {
//             Job.DeletedInvoiceId = selected.Id;
//             Console.WriteLine($"🗑️ Invoice ID marked for deletion: {selected.Id}");
//         }

//         Job.InvoiceEntries.Remove(selected);
//         OnPropertyChanged(nameof(Job.TotalInvoiceAmount));
//         Console.WriteLine("🗑️ Invoice removed from list.");
//     }
//     finally
//     {
//         _invoiceLock.Release();
//     }
// }




private async void AddPayment_Click(object sender, RoutedEventArgs e)
{
    await _paymentLock.WaitAsync();
    try
    {
        var entry = new PaymentModel
        {
            Id = Guid.NewGuid(),
            Date = DateTime.Now,
            TimeOnly = DateTime.Now.ToString("HH:mm:ss"),
            Method = "Cash",
            Description = "New Payment",
            Amount = 0m
        };
        Job.Payments.Add(entry);
        OnPropertyChanged(nameof(Job.TotalPaymentsAmount));
        Console.WriteLine("➕ Dummy payment row added.");
        // Optionally select the new row in the UI:
        PaymentsListView.SelectedItem = entry;
    }
    finally
    {
        _paymentLock.Release();
    }
}

//private readonly SemaphoreSlim _invoiceLock = new SemaphoreSlim(1, 1);
private readonly SemaphoreSlim _paymentLock = new SemaphoreSlim(1, 1);
        private async void EditPayment_Click(object sender, RoutedEventArgs e)
        {
            if (PaymentsListView.SelectedItem is not PaymentModel selected)
            {
                System.Windows.MessageBox.Show("Please select a payment to edit.");
                return;
            }

            await _paymentLock.WaitAsync();
            try
            {
                var editWindow = new EditPaymentWindow(selected)
                {
                    Owner = Window.GetWindow(this)
                };

                if (editWindow.ShowDialog() == true)
                {
                    if (DateTime.TryParseExact(
                            editWindow.Date + " " + editWindow.Time,
                            "yyyy-MM-dd HH:mm:ss",
                            null,
                            System.Globalization.DateTimeStyles.None,
                            out var parsedDateTime))
                    {
                        selected.Date = parsedDateTime;
                        selected.TimeOnly = editWindow.Time;
                    }
                    else
                    {
                        System.Windows.MessageBox.Show("Invalid date/time format.");
                        return;
                    }

                    selected.Method = editWindow.Method;
                    selected.Description = editWindow.Description;
                    selected.Amount = editWindow.Amount;

                    PaymentsListView.Items.Refresh();
                    OnPropertyChanged(nameof(Job.TotalPaymentsAmount));
                    Console.WriteLine("✏️ Payment updated.");
                }
            }
            finally
            {
                _paymentLock.Release();
            }
        }


private async void DeletePayment_Click(object sender, RoutedEventArgs e)
{
    if (PaymentsListView.SelectedItem is not PaymentModel selected)
    {
        System.Windows.MessageBox.Show("Please select a payment to delete.");
        return;
    }

    try
    {
        await Task.Run(() =>
        {
            lock (_paymentLock)
            {
                if (selected.Id != Guid.Empty)
                {
                    Job.DeletedPaymentId = selected.Id;
                }

                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    Job.Payments.Remove(selected);
                    OnPropertyChanged(nameof(Job.TotalPaymentsAmount));
                });
            }
        });

        Console.WriteLine("✅ Payment deleted successfully.");
    }
    catch (Exception ex)
    {
        System.Windows.MessageBox.Show($"Failed to delete payment: {ex.Message}", "Error", MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        Console.WriteLine($"❌ Exception in DeletePayment_Click: {ex.Message}");
    }
}


        private async void DeleteInvoice_Click(object sender, RoutedEventArgs e)
        {
            if (InvoiceListView.SelectedItem is not InvoiceModel selected)
                return;

            await _invoiceLock.WaitAsync();
            try
            {
                if (selected.Id != Guid.Empty)
                {
                    Job.DeletedInvoiceId = selected.Id;
                    Console.WriteLine($"🗑️ Invoice ID marked for deletion: {selected.Id}");
                }

                Job.InvoiceEntries.Remove(selected);
                OnPropertyChanged(nameof(Job.TotalInvoiceAmount));
                Console.WriteLine("🗑️ Invoice removed from list.");
            }
            finally
            {
                _invoiceLock.Release();
            }
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

private readonly SemaphoreSlim _invoiceEditLock = new SemaphoreSlim(1, 1);
private readonly SemaphoreSlim _defendantLock = new SemaphoreSlim(1, 1);
        private readonly SemaphoreSlim _plaintiffLock = new SemaphoreSlim(1, 1);

private readonly SemaphoreSlim _plaintiffsLock = new SemaphoreSlim(1, 1);




        private async void AddInvoices_Click(object sender, RoutedEventArgs e)
        {
            await _invoiceEditLock.WaitAsync();
            try
            {
                var entry = new CivilProcessERP.Models.Job.InvoiceModel();
                var popup = new EditInvoiceWindow(entry) { Owner = Window.GetWindow(this) };

                if (popup.ShowDialog() == true)
                {
                    // You can optionally add to Job.InvoiceEntries if needed
                    // Job.InvoiceEntries.Add(entry);

                    RecalculateTotals(); // 👈 Ensure totals update and UI refreshes
                    Console.WriteLine("➕ Invoice popup completed, totals recalculated.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"🔥 Error adding invoice: {ex.Message}");
            }
            finally
            {
                _invoiceEditLock.Release();
            }
        }

private async void EditDefendant_MouseDoubleClick(object sender, MouseButtonEventArgs e)
{
    await _defendantLock.WaitAsync();
    try
    {
        var parts = (Job.Defendant ?? "").Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        string firstName = parts.Length > 0 ? parts[0] : "";
        string lastName = parts.Length > 1 ? parts[1] : "";

        var dialog = new EditFieldDialog("Defendant", firstName, lastName);
        if (dialog.ShowDialog() == true)
        {
            Job.Defendant = $"{dialog.FirstName} {dialog.LastName}".Trim();
            OnPropertyChanged(nameof(Job.Defendant));
            Console.WriteLine($"✏️ Defendant name updated: {Job.Defendant}");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"🔥 Error editing defendant: {ex.Message}");
    }
    finally
    {
        _defendantLock.Release();
    }
}
        private async void EditPlaintiff_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            await _plaintiffLock.WaitAsync();
            try
            {
                var dialog = new EditPlaintiffSearchWindow(
                    "Host=localhost;Port=5432;Database=mypg_database;Username=postgres;Password=7866",
                    Job.Plaintiff)
                {
                    Owner = Window.GetWindow(this)
                };

                if (dialog.ShowDialog() == true)
                {
                    Job.Plaintiff = dialog.SelectedPlaintiffFullName;
                    Job.IsPlaintiffEdited = true;
                    OnPropertyChanged(nameof(Job.Plaintiff));
                    Console.WriteLine($"✏️ Plaintiff updated: {Job.Plaintiff}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"🔥 Error editing plaintiff: {ex.Message}");
            }
            finally
            {
                _plaintiffLock.Release();
            }
        }

private async void EditPlaintiffs_MouseDoubleClick(object sender, MouseButtonEventArgs e)
{
    await _plaintiffsLock.WaitAsync();
    try
    {
        var dialog = new EditPlaintiffsSearchWindow(
            "Host=localhost;Port=5432;Database=mypg_database;Username=postgres;Password=7866",
            Job.Plaintiffs)
        {
            Owner = Window.GetWindow(this)
        };

        if (dialog.ShowDialog() == true)
        {
            Job.Plaintiffs = dialog.SelectedPlaintiffsFullName;
            Job.IsPlaintiffsEdited = true;
            OnPropertyChanged(nameof(Job.Plaintiffs));
            Console.WriteLine($"✏️ Plaintiff updated: {Job.Plaintiffs}");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"🔥 Error editing plaintiff: {ex.Message}");
    }
    finally
    {
        _plaintiffsLock.Release();
    }
}

        private readonly SemaphoreSlim _attorneyLock = new SemaphoreSlim(1, 1);
private readonly SemaphoreSlim _processServerLock = new SemaphoreSlim(1, 1);
private readonly SemaphoreSlim _clientLock = new SemaphoreSlim(1, 1);

private async void EditAttorney_MouseDoubleClick(object sender, MouseButtonEventArgs e)
{
    await _attorneyLock.WaitAsync();
    try
    {
        var dialog = new EditAttorneySearchWindow(
            "Host=localhost;Port=5432;Database=mypg_database;Username=postgres;Password=7866",
            Job.Attorney)
        {
            Owner = Window.GetWindow(this)
        };

        if (dialog.ShowDialog() == true)
        {
            if (dialog.IsNewAttorney)
            {
                Job.Attorney = dialog.NewAttorneyFullName;
                Job.IsAttorneyNew = true;
            }
            else
            {
                Job.Attorney = dialog.SelectedAttorneyFullName;
                Job.IsAttorneyNew = false;
            }
            OnPropertyChanged(nameof(Job.Attorney));
            Console.WriteLine($"✏️ Attorney updated: {Job.Attorney}");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"🔥 Error editing attorney: {ex.Message}");
    }
    finally
    {
        _attorneyLock.Release();
    }
}

private async void EditProcessServer_MouseDoubleClick(object sender, MouseButtonEventArgs e)
{
    await _processServerLock.WaitAsync();
    try
    {
        var dialog2 = new EditProcessServerSearchWindow(
            "Host=localhost;Port=5432;Database=mypg_database;Username=postgres;Password=7866",
            Job.ProcessServer)
        {
            Owner = Window.GetWindow(this)
        };

        if (dialog2.ShowDialog() == true)
        {
            if (dialog2.IsNewProcessServer)
            {
                Job.ProcessServer = dialog2.NewProcessServerFullName; // <-- Use the correct property name
                Job.IsProcessServerNew = true;
            }
            else
            {
                Job.ProcessServer = dialog2.SelectedProcessServerFullName;
                Job.IsProcessServerNew = false;
            }
            OnPropertyChanged(nameof(Job.ProcessServer));
            Console.WriteLine($"✏️ Process server updated: {Job.ProcessServer}");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"🔥 Error editing process server: {ex.Message}");
    }
    finally
    {
        _processServerLock.Release();
    }
}
private async void EditClient_MouseDoubleClick(object sender, MouseButtonEventArgs e)
{
    await _clientLock.WaitAsync();
    try
    {
        var dialog3 = new EditClientSearchWindow(
            "Host=localhost;Port=5432;Database=mypg_database;Username=postgres;Password=7866",
            Job.Client)
        {
            Owner = Window.GetWindow(this)
        };

        if (dialog3.ShowDialog() == true)
        {
            if (dialog3.IsNewClient)
            {
                Job.Client = dialog3.NewClientFullName;
                Job.IsClientNew = true;
            }
            else
            {
                Job.Client = dialog3.SelectedClientFullName;
                Job.IsClientNew = false;
            }
            OnPropertyChanged(nameof(Job.Client));
            Console.WriteLine($"✏️ Client updated: {Job.Client}");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"🔥 Error editing client: {ex.Message}");
    }
    finally
    {
        _clientLock.Release();
    }
}



private async void EditClientStatus_MouseDoubleClick(object sender, MouseButtonEventArgs e)
{
    await _clientStatusLock.WaitAsync();
    try
    {
        var dialog4 = new SingleFieldDialog("Client Status", Job.ClientStatus);
        if (dialog4.ShowDialog() == true)
        {
            Job.ClientStatus = dialog4.Value;
            OnPropertyChanged(nameof(Job.ClientStatus));
            Console.WriteLine($"✏️ Client Status updated: {Job.ClientStatus}");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"🔥 Error editing client status: {ex.Message}");
    }
    finally
    {
        _clientStatusLock.Release();
    }
}


private async void EditArea_MouseDoubleClick(object sender, MouseButtonEventArgs e)
{
    await _areaLock.WaitAsync();
    try
    {
        var dialog5 = new EditAreaSearchWindow(
            "Host=localhost;Port=5432;Database=mypg_database;Username=postgres;Password=7866",
            Job.Zone)
        {
            Owner = Window.GetWindow(this)
        };

        if (dialog5.ShowDialog() == true)
        {
            Job.Zone = dialog5.SelectedArea;
            OnPropertyChanged(nameof(Job.Zone));
            Console.WriteLine($"✏️ Zone updated: {Job.Zone}");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"🔥 Error editing area: {ex.Message}");
    }
    finally
    {
        _areaLock.Release();
    }
}private async void EditTypeOfWrit_MouseDoubleClick(object sender, MouseButtonEventArgs e)
{
    await _typeOfWritLock.WaitAsync();
    try
    {
        var dialog6 = new EditTypeOfWritSearchWindow(Job.TypeOfWrit)
        {
            Owner = Window.GetWindow(this)
        };

        if (dialog6.ShowDialog() == true)
        {
            Job.TypeOfWrit = dialog6.SelectedTypeOfWrit;
            OnPropertyChanged(nameof(Job.TypeOfWrit));
            Console.WriteLine($"✏️ Type of Writ updated: {Job.TypeOfWrit}");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"🔥 Error editing type of writ: {ex.Message}");
    }
    finally
    {
        _typeOfWritLock.Release();
    }
}


private readonly SemaphoreSlim _clientStatusLock = new SemaphoreSlim(1, 1);
private readonly SemaphoreSlim _areaLock = new SemaphoreSlim(1, 1);
private readonly SemaphoreSlim _typeOfWritLock = new SemaphoreSlim(1, 1);
private readonly SemaphoreSlim _clientRefLock = new SemaphoreSlim(1, 1);

      private async void EditClientRef_MouseDoubleClick(object sender, MouseButtonEventArgs e)
{
    await _clientRefLock.WaitAsync();
    try
    {
        var dialog7 = new SingleFieldDialog("Client Reference", Job.ClientRef);
        if (dialog7.ShowDialog() == true)
        {
            Job.ClientRef = dialog7.Value;
            OnPropertyChanged(nameof(Job.ClientRef));
            Console.WriteLine($"✏️ Client Ref updated: {Job.ClientRef}");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"🔥 Error editing client ref: {ex.Message}");
    }
    finally
    {
        _clientRefLock.Release();
    }
}
// Removed duplicate EditServiceType_MouseDoubleClick to resolve method redefinition error.


private async void EditSQLDateTimeCreated_MouseDoubleClick(object sender, MouseButtonEventArgs e)
{
    await _editLock.WaitAsync();
    try
    {
        var sqlDateTimeCreated = DateTime.TryParse(Job.SqlDateTimeCreated, out var parsedDate) ? (DateTime?)parsedDate : null;
        var dialog = new EditDateDialog("SQL DateTime Created", sqlDateTimeCreated);
        if (dialog.ShowDialog() == true && dialog.SelectedDate != null)
        {
            Job.SqlDateTimeCreated = dialog.SelectedDate.Value.ToString("yyyy-MM-dd HH:mm:ss");
            OnPropertyChanged(nameof(Job.SqlDateTimeCreated));
        }
    }
    finally
    {
        _editLock.Release();
    }
}

private async void EditServiceType_MouseDoubleClick(object sender, MouseButtonEventArgs e)
{
    await _editLock.WaitAsync();
    try
    {
        var dialog9 = new EditServiceTypeSearchWindow("Host=localhost;Port=5432;Database=mypg_database;Username=postgres;Password=7866", Job.ServiceType)
        {
            Owner = Window.GetWindow(this)
        };

        if (dialog9.ShowDialog() == true)
        {
            Job.ServiceType = dialog9.SelectedServiceType;
            OnPropertyChanged(nameof(Job.ServiceType));
        }
    }
    finally
    {
        _editLock.Release();
    }
}

private async void EditTime_MouseDoubleClick(object sender, MouseButtonEventArgs e)
{
    await _editLock.WaitAsync();
    try
    {
        var dialog11 = new EditTimeDialog("Court Time", TimeSpan.TryParse(Job.Time, out var timeVal) ? timeVal : (TimeSpan?)null);
        if (dialog11.ShowDialog() == true && dialog11.SelectedTime.HasValue)
        {
            Job.Time = dialog11.SelectedTime.Value.ToString(@"hh\:mm");
            OnPropertyChanged(nameof(Job.Time));
        }
    }
    finally
    {
        _editLock.Release();
    }
}

private async void EditServiceTime_MouseDoubleClick(object sender, MouseButtonEventArgs e)
{
    await _editLock.WaitAsync();
    try
    {
        var dialog12 = new EditTimeDialog("Service Time", TimeSpan.TryParse(Job.ServiceTime, out var timeVal) ? timeVal : (TimeSpan?)null);
        if (dialog12.ShowDialog() == true && dialog12.SelectedTime.HasValue)
        {
            Job.ServiceTime = dialog12.SelectedTime.Value.ToString(@"hh\:mm");
            OnPropertyChanged(nameof(Job.ServiceTime));
        }
    }
    finally
    {
        _editLock.Release();
    }
}

private async void EditJobDate_MouseDoubleClick(object sender, MouseButtonEventArgs e)
{
    await _editLock.WaitAsync();
    try
    {
        var dialog13 = new EditDateDialog("Court Date", DateTime.TryParse(Job.Date, out var dateVal) ? dateVal : (DateTime?)null);
        if (dialog13.ShowDialog() == true && dialog13.SelectedDate.HasValue)
        {
            Job.Date = dialog13.SelectedDate.Value.ToString("MM/dd/yyyy");
            OnPropertyChanged(nameof(Job.Date));
        }
    }
    finally
    {
        _editLock.Release();
    }
}

private async void EditServiceDate_MouseDoubleClick(object sender, MouseButtonEventArgs e)
{
    await _editLock.WaitAsync();
    try
    {
        var dialog14 = new EditDateDialog("Service Date", DateTime.TryParse(Job.ServiceDate, out var dateVal) ? dateVal : (DateTime?)null);
        if (dialog14.ShowDialog() == true && dialog14.SelectedDate.HasValue)
        {
            Job.ServiceDate = dialog14.SelectedDate.Value.ToString("MM/dd/yyyy");
            OnPropertyChanged(nameof(Job.ServiceDate));
        }
    }
    finally
    {
        _editLock.Release();
    }
}

private static readonly SemaphoreSlim _editLock = new SemaphoreSlim(1, 1);

private async void EditExpirationDate_MouseDoubleClick(object sender, MouseButtonEventArgs e)
{
    await _editLock.WaitAsync();
    try
    {
        var existing = DateTime.TryParse(Job.ExpirationDate, out var parsed) ? parsed : (DateTime?)null;
        var dialog15 = new EditDateTimeDialog("Expiration Date", existing);
        if (dialog15.ShowDialog() == true && dialog15.SelectedDateTime.HasValue)
        {
            Job.ExpirationDate = dialog15.SelectedDateTime.Value.ToString("MM/dd/yyyy HH:mm");
            OnPropertyChanged(nameof(Job.ExpirationDate));
        }
    }
    finally
    {
        _editLock.Release();
    }
}

private async void EditLastDayToServe_MouseDoubleClick(object sender, MouseButtonEventArgs e)
{
    await _editLock.WaitAsync();
    try
    {
        var existing = DateTime.TryParse(Job.LastDayToServe, out var parsed) ? parsed : (DateTime?)null;
        var dialog16 = new EditDateTimeDialog("Last Day to Serve Date", existing);
        if (dialog16.ShowDialog() == true && dialog16.SelectedDateTime.HasValue)
        {
            Job.LastDayToServe = dialog16.SelectedDateTime.Value.ToString("MM/dd/yyyy HH:mm");
            OnPropertyChanged(nameof(Job.LastDayToServe));
        }
    }
    finally
    {
        _editLock.Release();
    }
}

private async void EditServeeAddress_MouseDoubleClick(object sender, MouseButtonEventArgs e)
{
    await _editLock.WaitAsync();
    try
    {
        string address1 = Job.AddressLine1 ?? "";
string address2 = Job.AddressLine2 ?? "";
string city     = Job.City ?? "";
string state    = Job.State ?? "";
string zip      = Job.Zip ?? "";

        var dialog = new EditServeeAddressWindow(address1, address2, city, state, zip)
        {
            Owner = Window.GetWindow(this)
        };

        if (dialog.ShowDialog() == true)
        {
            Job.Address = $"{dialog.Address1},{dialog.Address2},{dialog.City},{dialog.State},{dialog.Zip}";
            OnPropertyChanged(nameof(Job.Address));
            Console.WriteLine($"✏️ Servee Address updated: {Job.Address}");
        }
    }
    finally
    {
        _editLock.Release();
    }
}

private async void EditCourt_MouseDoubleClick(object sender, MouseButtonEventArgs e)
{
    await _editLock.WaitAsync();
    try
    {
        var dialog17 = new EditCourtSearchWindow(
            "Host=localhost;Port=5432;Database=mypg_database;Username=postgres;Password=7866",
            Job.Court)
        {
            Owner = Window.GetWindow(this)
        };

        if (dialog17.ShowDialog() == true)
        {
            Job.Court = dialog17.SelectedCourt;
            OnPropertyChanged(nameof(Job.Court));
        }
    }
    finally
    {
        _editLock.Release();
    }
}

private async void SaveButton_Click(object sender, RoutedEventArgs e)
{
    await _editLock.WaitAsync();
    try
    {
        var service = new JobService();
        await Task.Run(() => service.SaveJob(Job)); // Save in background thread
        System.Windows.MessageBox.Show("Job saved successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
    }
    catch (Exception ex)
    {
        System.Windows.MessageBox.Show($"Failed to save job: {ex.Message}", "Error", MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
    }
    finally
    {
        _editLock.Release();
    }
}

private async void CancelButton_Click(object sender, RoutedEventArgs e)
{
    await _editLock.WaitAsync();
    try
    {
        Job = _originalJob.Clone();

        DataContext = null;
        DataContext = this;

        System.Windows.MessageBox.Show("All unsaved changes were discarded.", "Cancelled", MessageBoxButton.OK, MessageBoxImage.Information);

        var mainWindow = Window.GetWindow(this) as MainWindow;
        mainWindow?.RemoveTab(this);
    }
    finally
    {
        _editLock.Release();
    }
}

private ObservableCollection<LogEntryModel> _attemptEntries;
public ObservableCollection<LogEntryModel> AttemptEntries
{
    get => _attemptEntries;
    set
    {
        _attemptEntries = value;
        OnPropertyChanged(nameof(AttemptEntries));
    }
}

private ObservableCollection<LogEntryModel> _commentEntries;
public ObservableCollection<LogEntryModel> CommentEntries
{
    get => _commentEntries;
    set
    {
        _commentEntries = value;
        OnPropertyChanged(nameof(CommentEntries));
    }
}

private void EditAttachmentPurpose_Click(object sender, RoutedEventArgs e)
{
    if (AttachmentsListView.SelectedItem is AttachmentModel selectedAttachment)
    {
        var editWindow = new EditAttachmentWindow(selectedAttachment) { Owner = Window.GetWindow(this) };
        if (editWindow.ShowDialog() == true)
        {
            selectedAttachment.Description = editWindow.Description;
            selectedAttachment.Format = editWindow.Format;
            selectedAttachment.Purpose = editWindow.Purpose;
            selectedAttachment.Status = "Edited";
            AttachmentsListView.Items.Refresh();
        }
    }
    else
    {
        System.Windows.MessageBox.Show("Please select an attachment to edit.", "No Selection", MessageBoxButton.OK, MessageBoxImage.Warning);
    }
}

}

}
 