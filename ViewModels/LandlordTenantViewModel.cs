using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Data;
using Npgsql;

using CivilProcessERP.Models.Job;
 // Assuming Job is in this namespace

namespace CivilProcessERP.ViewModels
{
    // public class LandlordTenantViewModel : INotifyPropertyChanged
    // {
    //     private readonly string connectionString = "Host=your_host;Port=5432;Username=your_user;Password=your_password;Database=your_database";

    //     private string _doNotFlyClients;
    //     private string _pastDueInvoices;
    //     private string _casesNeedAttention;

    //     public string DoNotFlyClients
    //     {
    //         get => _doNotFlyClients;
    //         set { _doNotFlyClients = value; OnPropertyChanged(); }
    //     }

    //     public string PastDueInvoices
    //     {
    //         get => _pastDueInvoices;
    //         set { _pastDueInvoices = value; OnPropertyChanged(); }
    //     }

    //     public string CasesNeedAttention
    //     {
    //         get => _casesNeedAttention;
    //         set { _casesNeedAttention = value; OnPropertyChanged(); }
    //     }

    //     public LandlordTenantViewModel()
    //     {
    //         LoadData();
    //     }

    //     private void LoadData()
    //     {
    //         using (var conn = new NpgsqlConnection(connectionString))
    //         {
    //             conn.Open();

    //             // Query to get DO NOT FLY Clients
    //             using (var cmd = new NpgsqlCommand("SELECT clients FROM do_not_fly_clients", conn))
    //             {
    //                 DoNotFlyClients = cmd.ExecuteScalar()?.ToString() ?? "No Data";
    //             }

    //             // Query to get past-due invoices
    //             using (var cmd = new NpgsqlCommand("SELECT COUNT(*) FROM invoices WHERE amount_due > 5000", conn))
    //             {
    //                 PastDueInvoices = cmd.ExecuteScalar()?.ToString() + " Clients";
    //             }

    //             // Query to get cases that need attention
    //             using (var cmd = new NpgsqlCommand("SELECT COUNT(*) FROM cases WHERE status = 'Needs Attention'", conn))
    //             {
    //                 CasesNeedAttention = cmd.ExecuteScalar()?.ToString() + " Cases";
    //             }
    //         }
    //     }

    //     public event PropertyChangedEventHandler PropertyChanged;
    //     protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
    //     {
    //         PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    //     }
    // }

// using System;
// using System.Collections.ObjectModel;
// using System.ComponentModel;
// using System.Runtime.CompilerServices;
// using System.Data;
// using Npgsql;

// namespace CivilProcessERP.ViewModels
// {
//     public class LandlordTenantViewModel : INotifyPropertyChanged
//     {
//         private readonly string connectionString = "Host=your_host;Port=5432;Username=your_user;Password=your_password;Database=your_database";

//         private string _doNotFlyClients;
//         private string _pastDueInvoices;
//         private string _casesNeedAttention;
//         private string _searchJobNumber;
//         private string _searchResult;

//         public string DoNotFlyClients
//         {
//             get => _doNotFlyClients;
//             set { _doNotFlyClients = value; OnPropertyChanged(); }
//         }

//         public string PastDueInvoices
//         {
//             get => _pastDueInvoices;
//             set { _pastDueInvoices = value; OnPropertyChanged(); }
//         }

//         public string CasesNeedAttention
//         {
//             get => _casesNeedAttention;
//             set { _casesNeedAttention = value; OnPropertyChanged(); }
//         }

//         public string SearchJobNumber
//         {
//             get => _searchJobNumber;
//             set { _searchJobNumber = value; OnPropertyChanged(); }
//         }

//         public string SearchResult
//         {
//             get => _searchResult;
//             set { _searchResult = value; OnPropertyChanged(); }
//         }

//         public ObservableCollection<string> JobList { get; set; } = new ObservableCollection<string>();

//         public LandlordTenantViewModel()
//         {
//             LoadData();
//         }

//         private void LoadData()
//         {
//             using (var conn = new NpgsqlConnection(connectionString))
//             {
//                 conn.Open();

//                 // Query to get DO NOT FLY Clients
//                 using (var cmd = new NpgsqlCommand("SELECT clients FROM do_not_fly_clients", conn))
//                 {
//                     DoNotFlyClients = cmd.ExecuteScalar()?.ToString() ?? "No Data";
//                 }

//                 // Query to get past-due invoices
//                 using (var cmd = new NpgsqlCommand("SELECT COUNT(*) FROM invoices WHERE amount_due > 5000", conn))
//                 {
//                     PastDueInvoices = cmd.ExecuteScalar()?.ToString() + " Clients";
//                 }

//                 // Query to get cases that need attention
//                 using (var cmd = new NpgsqlCommand("SELECT COUNT(*) FROM cases WHERE status = 'Needs Attention'", conn))
//                 {
//                     CasesNeedAttention = cmd.ExecuteScalar()?.ToString() + " Cases";
//             }
//         }
//         }

//         // ✅ Search Job Functionality (Placeholder Logic for Now)
//         public void SearchJob()
//         {
//             if (string.IsNullOrWhiteSpace(SearchJobNumber))
//             {
//                 SearchResult = "Please enter a job number.";
//                 return;
//             }

//             // Future implementation: Search the database
//             Console.WriteLine($"[INFO] Searching for Job: {SearchJobNumber}");
//             SearchResult = $"Job {SearchJobNumber} details found (Mock Data)";
//         }

//         // ✅ Add Job Functionality (Placeholder)
//         public void AddNewJob()
//         {
//             string newJob = $"New Job {DateTime.Now.Ticks}";
//             JobList.Add(newJob);

//             // Future: Insert into database
//             Console.WriteLine($"[INFO] Job Added: {newJob}");
//         }

//         public event PropertyChangedEventHandler PropertyChanged;
//         protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
//         {
//             PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
//         }
//     }
// }
// }


// using System;
// using System.Collections.ObjectModel;
// using System.ComponentModel;
// using System.Runtime.CompilerServices;
// using System.Data;
// using Npgsql;


// namespace CivilProcessERP.ViewModels
// {
//     public class LandlordTenantViewModel : INotifyPropertyChanged
//     {
//         // 🚀 Flag to Disable Database Calls for Now
//         private readonly bool UseDatabase = false;  

//         private string _doNotFlyClients;
//         private string _pastDueInvoices;
//         private string _casesNeedAttention;
//         private string _searchJobNumber;
//         private string _searchResult;

//         public string DoNotFlyClients
//         {
//             get => _doNotFlyClients;
//             set { _doNotFlyClients = value; OnPropertyChanged(); }
//         }

//         public string PastDueInvoices
//         {
//             get => _pastDueInvoices;
//             set { _pastDueInvoices = value; OnPropertyChanged(); }
//         }

//         public string CasesNeedAttention
//         {
//             get => _casesNeedAttention;
//             set { _casesNeedAttention = value; OnPropertyChanged(); }
//         }

//         public string SearchJobNumber
//         {
//             get => _searchJobNumber;
//             set { _searchJobNumber = value; OnPropertyChanged(); }
//         }

//         public string SearchResult
//         {
//             get => _searchResult;
//             set { _searchResult = value; OnPropertyChanged(); }
//         }

//         public ObservableCollection<string> JobList { get; set; } = new ObservableCollection<string>();

//         public LandlordTenantViewModel()
//         {
//             // 🚀 Only Load Data When DB is Enabled
//             if (UseDatabase)
//             {
//                 LoadData();
//             }
//             else
//             {
//                 LoadMockData();
//             }
//         }

//         private void LoadMockData()
//         {
//             Console.WriteLine("[INFO] Using Mock Data instead of DB.");
//             DoNotFlyClients = "Mock Client A, Mock Client B";
//             PastDueInvoices = "3 Clients";
//             CasesNeedAttention = "5 Cases";
//         }

//         private void LoadData()
//         {
//             if (!UseDatabase)
//                 return;

//             // 🚀 Future: Implement DB Connection Here
//             Console.WriteLine("[INFO] Database Connection will be Implemented Later.");
//         }

//         // ✅ Search Job Functionality (Mocked)
//         public void SearchJob()
//         {
//             if (string.IsNullOrWhiteSpace(SearchJobNumber))
//             {
//                 SearchResult = "Please enter a job number.";
//                 return;
//             }

//             Console.WriteLine($"[INFO] Searching for Job: {SearchJobNumber}");
//             SearchResult = $"Job {SearchJobNumber} details found (Mock Data)";
//         }

//         // ✅ Add Job Functionality (Mocked)
//         public void AddNewJob()
//         {
//             string newJob = $"New Job {DateTime.Now.Ticks}";
//             JobList.Add(newJob);
//             Console.WriteLine($"[INFO] Job Added: {newJob}");
//         }

//         public event PropertyChangedEventHandler PropertyChanged;
//         protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
//         {
//             PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
//         }
//     }
// }
// }
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using System.Windows;
using CivilProcessERP.ViewModels; // Ensures TabItemViewModel is referenced
    using global::CivilProcessERP.Views;

    namespace CivilProcessERP.ViewModels
{
    public class LandlordTenantViewModel : INotifyPropertyChanged
    {
        private readonly bool UseDatabase = false;

        private string _doNotFlyClients;
        private string _pastDueInvoices;
        private string _casesNeedAttention;
        private string _searchJobNumber;
        private string _searchResult;
        private Job _mockJob;

        public string DoNotFlyClients
        {
            get => _doNotFlyClients;
            set { _doNotFlyClients = value; OnPropertyChanged(); }
        }

        public string PastDueInvoices
        {
            get => _pastDueInvoices;
            set { _pastDueInvoices = value; OnPropertyChanged(); }
        }

        public string CasesNeedAttention
        {
            get => _casesNeedAttention;
            set { _casesNeedAttention = value; OnPropertyChanged(); }
        }

        public string SearchJobNumber
        {
            get => _searchJobNumber;
            set { _searchJobNumber = value; OnPropertyChanged(); }
        }

        public string SearchResult
        {
            get => _searchResult;
            set { _searchResult = value; OnPropertyChanged(); }
        }

        public Job MockJob
        {
            get => _mockJob;
            set { _mockJob = value; OnPropertyChanged(); }
        }

        public ObservableCollection<string> JobList { get; set; } = new ObservableCollection<string>();

        public LandlordTenantViewModel()
        {
            if (UseDatabase)
            {
                LoadData();
            }
            else
            {
                LoadMockData();
            }
        }

        private void LoadMockData()
        {
            Console.WriteLine("[INFO] Using Mock Data instead of DB.");
            DoNotFlyClients = "Mock Client A, Mock Client B";
            PastDueInvoices = "3 Clients";
            CasesNeedAttention = "5 Cases";

            // ✅ Initialize Mock Job Data
            MockJob = new Job
            {
                JobId = "143", // Set a job number for testing
                Court = "Michigan District Court",
                Plaintiff = "John Doe",
                Defendant = "Jane Smith",
                Address = "123 Main St, Ann Arbor, MI",
                Status = "Pending",
                CaseNumber = "2024-XY-56789",
                ClientReference = "CL-56789",
                TypeOfWrit = "SUMMONS LANDLORD TENANT",
                ServiceType = "Standard",
                Date = "04/10/2025",
                Time = "10:00 AM",
                InvoiceDue = "$500",
                ClientStatus = "Active",
                Zone = "West",
                LastServiceDate = "04/15/2025"
            };
        }

        private void LoadData()
        {
            if (!UseDatabase)
                return;

            Console.WriteLine("[INFO] Database Connection will be Implemented Later.");
        }

        // ✅ Fixed: Search Job & Open in a New Tab
        public void SearchJob()
        {
            if (string.IsNullOrWhiteSpace(SearchJobNumber))
            {
                SearchResult = "Please enter a job number.";
                return;
            }

            Console.WriteLine($"[INFO] Searching for Job: {SearchJobNumber}");

            // ✅ Check if job exists in mock data
            if (MockJob != null && MockJob.JobId == SearchJobNumber)
            {
                // ✅ Open Job Details View if match is found
                Application.Current.Dispatcher.Invoke(() =>
                {
                    var jobDetailsView = new JobDetailsView(MockJob);
                    
                    // Find MainWindow
                    var mainWindow = Application.Current.MainWindow as MainWindow;
                    
                    if (mainWindow != null)
                    {
                        // Ensure DataContext is set to MainWindow ViewModel
                        var mainViewModel = mainWindow.DataContext as MainDashboardViewModel;
                        
                        if (mainViewModel != null)
                        {
                            // ✅ Open Job Details in a New Tab
                            mainViewModel.OpenNewTab(MockJob);
                        }
                    }
                });

                SearchResult = $"Job {SearchJobNumber} found (Mock Data Loaded)";
            }
            else
            {
                SearchResult = $"Job {SearchJobNumber} not found.";
            }
        }

        // ✅ Add Job Functionality (Mocked)
        public void AddNewJob()
        {
            string newJob = $"New Job {DateTime.Now.Ticks}";
            JobList.Add(newJob);
            Console.WriteLine($"[INFO] Job Added: {newJob}");
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
}