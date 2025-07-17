using System.Collections.ObjectModel;
using System.ComponentModel;

namespace CivilProcessERP.Models.Job
{
    public class Job : INotifyPropertyChanged
    {    
        //public decimal TotalInvoiceAmount { get; set; }
        public Guid? DeletedInvoiceId { get; set; } = null;
        public Guid? DeletedPaymentId { get; set; }
        public Guid? DeletedAttachmentId { get; set; }



        // Remove legacy string date fields
        // public string ExpirationDate { get; set; }
        // public string SqlDateTimeCreated { get; set; }
        // public string LastDayToServe { get; set; }
        // public string Date { get; set; }
        // public string Time { get; set; }
        // public string ServiceDate { get; set; }
        // public string ServiceTime { get; set; }
        // public string LastServiceDate { get; set; }
        // Add new DateTime? fields
        public DateTime? ExpirationDate { get; set; }
        public DateTime? SqlDateTimeCreated { get; set; }
        public DateTime? LastDayToServe { get; set; }
        // Remove legacy fields for court date/time and service date/time
        // public string Date { get; set; }
        // public string Time { get; set; }
        // public string ServiceDate { get; set; }
        // public string ServiceTime { get; set; }
        // Add new DateTime? fields for these only
        public DateTime? CourtDateTime { get; set; } // replaces Date/Time
        public DateTime? ServiceDateTime { get; set; } // replaces ServiceDate/ServiceTime
        // Display helpers
        public string SqlDateTimeCreatedDisplay => SqlDateTimeCreated.HasValue ? SqlDateTimeCreated.Value.ToString("yyyy-MM-dd HH:mm") : "N/A";
        public string LastDayToServeDisplay => LastDayToServe.HasValue ? LastDayToServe.Value.ToString("yyyy-MM-dd HH:mm") : "N/A";
        public string ExpirationDateDisplay => ExpirationDate.HasValue ? ExpirationDate.Value.ToString("yyyy-MM-dd HH:mm") : "N/A";
        public string CourtDateDisplay => CourtDateTime.HasValue && CourtDateTime.Value.Year > 1972 ? CourtDateTime.Value.ToString("yyyy-MM-dd") : "N/A";
        public string CourtTimeDisplay => CourtDateTime.HasValue && CourtDateTime.Value.Year > 1972 ? CourtDateTime.Value.ToString("HH:mm") : "N/A";
        public string ServiceDateDisplay => ServiceDateTime.HasValue && ServiceDateTime.Value.Year > 1972 ? ServiceDateTime.Value.ToString("yyyy-MM-dd") : "N/A";
        public string ServiceTimeDisplay => ServiceDateTime.HasValue && ServiceDateTime.Value.Year > 1972 ? ServiceDateTime.Value.ToString("HH:mm") : "N/A";

        public string CaseSerial { get; set; } = string.Empty;  // This holds the numeric `cases.serialnum`
public string CaseNumber { get; set; } = string.Empty;  // This holds the public display number e.g., "2230638GC"

        public string JobId { get; set; } = string.Empty;
        public string Court { get; set; } = string.Empty;
        public string Plaintiff { get; set; } = string.Empty;

        public string Plaintiffs { get; set; } = string.Empty;
        public string Defendant { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;

        public string AddressLine1 { get; set; } = string.Empty;
public string AddressLine2 { get; set; } = string.Empty;
public string City { get; set; } = string.Empty;
public string State { get; set; } = string.Empty;
public string Zip { get; set; } = string.Empty;

        public string? Status { get; set; }
        //public string CaseNumber { get; set; }
        public string ClientReference { get; set; } = string.Empty;
        public string TypeOfWrit { get; set; } = string.Empty;
        public string ServiceType { get; set; } = string.Empty;
        //public string InvoiceDue { get; set; }
        public string ClientStatus { get; set; } = string.Empty;
        public string Zone { get; set; } = string.Empty;
        //public string LastServiceDate { get; set; }

        public string CourtSerial { get; set; } = string.Empty;

        public string JobNumber { get; set; } = string.Empty;
        public string ClientRef { get; set; } = string.Empty;
        public string Attorney { get; set; } = string.Empty;
        public string TypeOfService { get; set; } = string.Empty;
        public DateTime? DateOfService { get; set; }
        //public List<AttachmentModel> Attachments { get; set; } = new List<AttachmentModel>();

        public List<ChangeEntryModel> ChangeHistory { get; set; } = new List<ChangeEntryModel>();
         //public List<PaymentModel> PaymentEntries { get; set; } = new List<PaymentModel>();
        
        public ObservableCollection<InvoiceModel> InvoiceEntries { get; set; } = new ObservableCollection<InvoiceModel>();


        //public decimal TotalInvoiceAmount => InvoiceEntries?.Sum(x => x.Amount) ?? 0;
        // Removed duplicate PaymentEntries property to resolve the error.
        // public ObservableCollection<PaymentEntryModel> PaymentEntries { get; set; } = new ObservableCollection<PaymentEntryModel>();

        public ObservableCollection<PaymentModel> Payments { get; set; } = new ObservableCollection<PaymentModel>();
        public ObservableCollection<AttachmentModel> Attachments { get; set; } = new ObservableCollection<AttachmentModel>();

        public object? AdditionalComments { get; internal set; } = null;
        //public List<LogEntryModel> Comments { get; set; } = new List<LogEntryModel>();
        public ObservableCollection<CommentModel> Comments { get; set; } = new();
        public ObservableCollection<AttemptsModel> Attempts { get; set; } = new ();

        public decimal TotalInvoiceAmount => InvoiceEntries?.Sum(x => x.Amount) ?? 0;
        public decimal TotalPaymentsAmount => Payments?.Sum(x => x.Amount) ?? 0;

         public Job Clone()
    {
        return new Job
        {
            JobId = this.JobId,
            Court = this.Court,
            Defendant = this.Defendant,
            Plaintiffs = this.Plaintiffs,
            Plaintiff = this.Plaintiff,
            Address = this.Address,
            Zone = this.Zone,
            SqlDateTimeCreated = this.SqlDateTimeCreated,
            ExpirationDate = this.ExpirationDate,
            LastDayToServe = this.LastDayToServe,
            TypeOfWrit = this.TypeOfWrit,
            ClientReference = this.ClientReference,
            ServiceType = this.ServiceType,
            CourtDateTime = this.CourtDateTime,
            ServiceDateTime = this.ServiceDateTime,
            Attorney = this.Attorney,
            Client = this.Client,
            ProcessServer = this.ProcessServer,
            ClientStatus = this.ClientStatus,
            CaseNumber = this.CaseNumber,
            // ❗ DO NOT CLONE InvoiceDue – it's a computed property
            InvoiceEntries = new ObservableCollection<InvoiceModel>(this.InvoiceEntries.Select(x => x.Clone())),
            Payments = new ObservableCollection<PaymentModel>(this.Payments.Select(x => x.Clone())),
            Comments = new ObservableCollection<CommentModel>(this.Comments.Select(x => x.Clone())),
            Attempts = new ObservableCollection<AttemptsModel>(this.Attempts.Select(x => x.Clone())),
            ChangeHistory = new List<ChangeEntryModel>(this.ChangeHistory),
            Attachments = new ObservableCollection<AttachmentModel>(this.Attachments.Select(x => x.Clone()))
        };
    }
public string InvoiceDue 
{
    get
    {
        // Calculate the total invoice and total payments and return the difference (Invoice - Payments)
        decimal invoiceDue = TotalInvoiceAmount - TotalPaymentsAmount;

        // Check if the value is negative and adjust the formatting
        if (invoiceDue < 0)
        {
            return "-" + Math.Abs(invoiceDue).ToString("C"); // Manually add the minus sign for negative values
        }
        else
        {
            return invoiceDue.ToString("C"); // Standard currency format for positive values
        }
    }
}




public event PropertyChangedEventHandler? PropertyChanged;

protected void OnPropertyChanged(string propertyName)
{
    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
        
  
        // Removed duplicate Attachments property to resolve the error.

        
        //public ObservableCollection<LogEntryModel> AttemptEntries { get; set; } = new();

        
        public string? Client { get; set; } = null;
        public string? ProcessServer { get; set; } = null;

        // New properties
    public string? ServiceDate { get; set; } = null;
    public string? ServiceTime { get; set; } = null;
        public object? AttachmentsModel { get; internal set; } = null;
        public bool IsPlaintiffsEdited { get; set; }
        public bool IsPlaintiffEdited { get; set; }
        // Add these for AddJobView support:
        public bool IsPlaintiffNew { get; set; }
        public bool IsPlaintiffsNew { get; set; }
        public bool IsAttorneyNew { get; set; }
        public bool IsProcessServerNew { get; set; }
        public bool IsClientNew { get; set; }
        public bool IsAttorneyEdited { get; set; }
        public bool IsProcessServerEdited { get; set; }
        public bool IsClientEdited { get; set; }
        public bool WorkflowFCM { get; set; }
        public bool WorkflowSOPS { get; set; }
        public bool WorkflowIIA { get; set; }
    }

    public class InvoiceModel
    {
        public Guid Id { get; set; }
       public string Description { get; set; } = string.Empty; 
        public int Quantity { get; set; }
        public decimal Rate { get; set; }
        public decimal Amount { get; set; }
    public InvoiceModel Clone()
    {
        return new InvoiceModel
        {
            Id = this.Id,
            Description = this.Description,
            Quantity = this.Quantity,
            Rate = this.Rate,
            Amount = this.Amount
        };
    }
}


 public class PaymentModel
{
    public Guid Id { get; set; }
    public DateTime? DateTime { get; set; }
    public string DateDisplay => DateTime?.ToString("yyyy-MM-dd") ?? "N/A";
    public string TimeDisplay => DateTime?.ToString("HH:mm:ss") ?? "N/A";
    public string Description { get; set; } = string.Empty;
    public string Method { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public PaymentModel Clone()
    {
        return new PaymentModel
        {
            Id = this.Id,
            DateTime = this.DateTime,
            Description = this.Description,
            Method = this.Method,
            Amount = this.Amount
        };
    }
}




    public class CommentModel
{
    public int Seqnum { get; set; }
    public DateTime? DateTime { get; set; }
    public string DateDisplay => DateTime?.ToString("yyyy-MM-dd") ?? "N/A";
    public string TimeDisplay => DateTime?.ToString("HH:mm:ss") ?? "N/A";
    public string Body { get; set; } = string.Empty;

    
        public bool Aff { get; set; }
        public bool FS { get; set; }
        public bool Att { get; set; }


    public string Source { get; set; } = string.Empty;
    
     public long SerialNum { get; set; }

    public CommentModel Clone()
        {
            return new CommentModel
            {
                SerialNum = this.SerialNum,
                Seqnum = this.Seqnum,
                DateTime = this.DateTime,
                Body = this.Body,
                Aff = this.Aff,
                FS = this.FS,
                Att = this.Att,
                Source = this.Source
            };
        }
}

    public class AttemptsModel
{
public long  SerialNum { get; set; }

public int Seqnum { get; set; }
        public DateTime? DateTime { get; set; }
        public string DateDisplay => DateTime?.ToString("yyyy-MM-dd") ?? "N/A";
        public string TimeDisplay => DateTime?.ToString("HH:mm:ss") ?? "N/A";
    public string Body { get; set; } = string.Empty;

    
        public bool Aff { get; set; }
        public bool FS { get; set; }
        public bool Att { get; set; }


    public string Source { get; set; } = string.Empty;
    public AttemptsModel Clone()
    {
        return new AttemptsModel
        {
            SerialNum = this.SerialNum,
            Seqnum = this.Seqnum,
            DateTime = this.DateTime,
            Body = this.Body,
            Aff = this.Aff,
            FS = this.FS,
            Att = this.Att,
            Source = this.Source
        };
    }
}

public class AttachmentModel
{
    public Guid Id { get; set; }              // ✅ Unique ID for attachment
    public Guid BlobId { get; set; }          // ✅ ID for blob storage
    public string Purpose { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Format { get; set; } = string.Empty;
    public string FileExtension { get; set; } = string.Empty;
    public byte[] FileData { get; set; } = Array.Empty<byte>();      // ✅ Stores binary file data
    public string BlobMetadataId { get; set; } = string.Empty; // ✅ Optional: string identifier for blobmetadata

    public string FilePath { get; set; } = string.Empty;      // ✅ Local path used for display/editing (not stored in DB)
    public string Status { get; set; } = string.Empty;  
    public string Filename { get; set; } = string.Empty;      // ✅ "New", "Edited", "Synced", etc.

    public AttachmentModel Clone()
    {
        return new AttachmentModel
        {
            Id = this.Id,
            BlobId = this.BlobId,
            Purpose = this.Purpose,
            Description = this.Description,
            Format = this.Format,
            FileExtension = this.FileExtension,
            FileData = this.FileData,
            Filename = this.Filename,
            BlobMetadataId = this.BlobMetadataId,
            FilePath = this.FilePath,
            Status = this.Status
        };
    }
}

public class ChangeEntryModel
{
    public DateTime Date { get; set; }
    public string FieldName { get; set; } = string.Empty; // Action (e.g., "SEARCH")
    public string OldValue { get; set; } = string.Empty;  // Username
    public string NewValue { get; set; } = string.Empty;  // Details
    public string ChangedBy { get; set; } = string.Empty;
}

}


