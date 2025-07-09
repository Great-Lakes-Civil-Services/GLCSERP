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



        public string ExpirationDate { get; set; }
        public string SqlDateTimeCreated { get; set; }
        public string LastDayToServe { get; set; }

        public string CaseSerial { get; set; }  // This holds the numeric `cases.serialnum`
public string CaseNumber { get; set; }  // This holds the public display number e.g., "2230638GC"

        public string JobId { get; set; }
        public string Court { get; set; }
        public string Plaintiff { get; set; }

        public string Plaintiffs { get; set; }
        public string Defendant { get; set; }
        public string Address { get; set; }

        public string AddressLine1 { get; set; }
public string AddressLine2 { get; set; }
public string City { get; set; }
public string State { get; set; }
public string Zip { get; set; }

        public string? Status { get; set; }
        //public string CaseNumber { get; set; }
        public string ClientReference { get; set; }
        public string TypeOfWrit { get; set; }
        public string ServiceType { get; set; }
        public string Date { get; set; }
        public string Time { get; set; }
        //public string InvoiceDue { get; set; }
        public string ClientStatus { get; set; }
        public string Zone { get; set; }
        public string LastServiceDate { get; set; }

        public string CourtSerial { get; set; }

        public string JobNumber { get; set; }
        public string ClientRef { get; set; }
        public string Attorney { get; set; }
        public string TypeOfService { get; set; }
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

        public object AdditionalComments { get; internal set; }
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
            ServiceDate = this.ServiceDate,
            ServiceTime = this.ServiceTime,
            Date = this.Date,
            Time = this.Time,
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




public event PropertyChangedEventHandler PropertyChanged;

protected void OnPropertyChanged(string propertyName)
{
    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
        
  
        // Removed duplicate Attachments property to resolve the error.

        
        //public ObservableCollection<LogEntryModel> AttemptEntries { get; set; } = new();

        
        public string Client { get; set; }
        public string ProcessServer { get; set; }

        // New properties
    public string ServiceDate { get; set; }
    public string ServiceTime { get; set; }
        public object AttachmentsModel { get; internal set; }
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
    }

    public class InvoiceModel
    {
        public Guid Id { get; set; }
       public string Description { get; set; } 
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
    public DateTime Date { get; set; }
    public string Description { get; set; }
    public string Method { get; set; }
    public decimal Amount { get; set; }
    public string TimeOnly { get; internal set; }

    public PaymentModel Clone()
    {
        return new PaymentModel
        {
            Id = this.Id,
            Date = this.Date,
            Description = this.Description,
            Method = this.Method,
            Amount = this.Amount,
            TimeOnly = this.TimeOnly
        };
    }
}




    public class CommentModel
{
    public int Seqnum { get; set; }
    public string Date { get; set; }
    public string Time { get; set; }
    public string Body { get; set; }

    
        public bool Aff { get; set; }
        public bool FS { get; set; }
        public bool Att { get; set; }


    public string Source { get; set; }
    
     public long SerialNum { get; set; }

    public CommentModel Clone()
        {
            return new CommentModel
            {
                SerialNum = this.SerialNum,
                Seqnum = this.Seqnum,
                Date = this.Date,
                Time = this.Time,
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
        public string Date { get; set; }
    public string Time { get; set; }
    public string Body { get; set; }

    
        public bool Aff { get; set; }
        public bool FS { get; set; }
        public bool Att { get; set; }


    public string Source { get; set; }
    public AttemptsModel Clone()
    {
        return new AttemptsModel
        {
            SerialNum = this.SerialNum,
            Seqnum = this.Seqnum,
            Date = this.Date,
            Time = this.Time,
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
    public string Purpose { get; set; }
    public string Description { get; set; }
    public string Format { get; set; }
    public string FileExtension { get; set; }
    public byte[] FileData { get; set; }      // ✅ Stores binary file data
    public string BlobMetadataId { get; set; } // ✅ Optional: string identifier for blobmetadata

    public string FilePath { get; set; }      // ✅ Local path used for display/editing (not stored in DB)
    public string Status { get; set; }  
    public string Filename { get; set; }      // ✅ "New", "Edited", "Synced", etc.

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
    public string FieldName { get; set; } // Action (e.g., "SEARCH")
    public string OldValue { get; set; }  // Username
    public string NewValue { get; set; }  // Details
}

}


