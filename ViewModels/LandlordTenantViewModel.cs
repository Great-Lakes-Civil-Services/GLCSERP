using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using CivilProcessERP.Models.Job;
using CivilProcessERP.Views;

namespace CivilProcessERP.ViewModels
{
    public class LandlordTenantViewModel : INotifyPropertyChanged
    {
        public bool UseDatabase { get; set; } = true;

        private string? _doNotFlyClients = null;
        private string? _pastDueInvoices = null;
        private string? _casesNeedAttention = null;
        private string? _searchJobNumber = null;
        private string? _searchResult = null;
        private Job? _currentJob = null;

        private CancellationTokenSource? _cts = null;

        private Job? _selectedJob = null;
        public Job SelectedJob
        {
            get => _selectedJob ?? new Job(); // Return a default Job if null
            set 
            { 
                _selectedJob = value; 
                OnPropertyChanged();
                Console.WriteLine($"[DEBUG] SelectedJob changed to: {_selectedJob?.JobId}");
            }
        }

        public string DoNotFlyClients
        {
            get => _doNotFlyClients ?? string.Empty;
            set { _doNotFlyClients = value; OnPropertyChanged(); }
        }

        public string PastDueInvoices
        {
            get => _pastDueInvoices ?? string.Empty;
            set { _pastDueInvoices = value; OnPropertyChanged(); }
        }

        public string CasesNeedAttention
        {
            get => _casesNeedAttention ?? string.Empty;
            set { _casesNeedAttention = value; OnPropertyChanged(); }
        }

        public string SearchJobNumber
        {
            get => _searchJobNumber ?? string.Empty;
            set
            {
                _searchJobNumber = value;
                Console.WriteLine($"[BINDING] SearchJobNumber set to: {_searchJobNumber}");
                OnPropertyChanged();
            }
        }

        public string SearchResult
        {
            get => _searchResult ?? string.Empty;
            set { _searchResult = value; OnPropertyChanged(); }
        }

        public Job CurrentJob
        {
            get => _currentJob ?? new Job(); // Return a default Job if null
            set { _currentJob = value; OnPropertyChanged(); }
        }

        public ObservableCollection<string> JobList { get; set; } = new();
        public ObservableCollection<Job> FilteredJobs { get; set; } = new ObservableCollection<Job>();

        public LandlordTenantViewModel(Job job, bool useDb = true)
{
    UseDatabase = useDb;
    SelectedJob = job;
    SearchJobNumber = job.JobId;
    CurrentJob = job;

    PropertyChanged += (s, e) =>
    {
        Console.WriteLine($"[ViewModel] PropertyChanged: {e.PropertyName}");
    };

    Console.WriteLine($"[INFO] LandlordTenantViewModel created for JobId: {job.JobId}");
}

        private void LoadMockData()
        {
            Console.WriteLine("[INFO] Using Mock Data instead of DB.");
            DoNotFlyClients = "Mock Client A, Mock Client B";
            PastDueInvoices = "3 Clients";
            CasesNeedAttention = "5 Cases";

            CurrentJob = new Job
            {
                JobId = "143",
                Court = "Michigan District Court",
                Plaintiff = "John Doe",
                Defendant = "Jane Smith",
                Address = "123 Main St, Ann Arbor, MI",
                Status = "Pending",
                CaseNumber = "2024-XY-56789",
                ClientReference = "CL-56789",
                TypeOfWrit = "SUMMONS LANDLORD TENANT",
                ServiceType = "Standard",
                CourtDateTime = new DateTime(2025, 4, 10, 10, 0, 0),
                ServiceDateTime = new DateTime(2025, 4, 15, 14, 0, 0),
                ClientStatus = "Active",
                Zone = "West"
            };
        }

        public async Task<Job?> LoadDataAsync(string jobId, CancellationToken token = default)
        {
            var jobService = new JobService();
            return await jobService.GetJobById(jobId);
        }

       public async Task<Job?> SearchJobAsync()
{
    Console.WriteLine($"[DEBUG] Searching for Job: {SearchJobNumber}");

    if (string.IsNullOrWhiteSpace(SearchJobNumber))
    {
        SearchResult = "Please enter a job number.";
        FilteredJobs.Clear();
        return null;
    }

    _cts?.Cancel();
    _cts = new CancellationTokenSource();

    try
    {
        SearchResult = "Searching...";
        var dbJob = await LoadDataAsync(SearchJobNumber, _cts.Token);

        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            FilteredJobs.Clear();
            if (dbJob != null)
            {
                 Console.WriteLine($"[DEBUG] ✅ SearchJobAsync returned job: {dbJob.JobId}");
    FilteredJobs.Clear();
                FilteredJobs.Add(dbJob);
                SelectedJob = dbJob;
                SearchResult = $"✅ Job {SearchJobNumber} loaded.";
            }
            else
            {
                SearchResult = "No job found.";
            }
        });

        return dbJob;
    }
    catch (OperationCanceledException)
    {
        SearchResult = "🔄 Search canceled.";
        FilteredJobs.Clear();
        return null;
    }
    catch (Exception ex)
    {
        SearchResult = $"🔥 Error: {ex.Message}";
        FilteredJobs.Clear();
        return null;
    }
}

        public void AddNewJob()
        {
            string newJob = $"New Job {DateTime.Now.Ticks}";
            JobList.Add(newJob);
            Console.WriteLine($"[INFO] Job Added: {newJob}");
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName ?? string.Empty));

        private void AddDummy_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.DataContext is LandlordTenantViewModel vm)
            {
                vm.FilteredJobs.Add(new Job { JobId = "TEST", CaseNumber = "CASE123" });
            }
        }
    }
}
