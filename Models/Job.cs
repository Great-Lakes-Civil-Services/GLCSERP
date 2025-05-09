
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace CivilProcessERP.Models.Job
{
    public class Job : INotifyPropertyChanged
    {    
        //public decimal TotalInvoiceAmount { get; set; }
        public string ExpirationDate { get; set; }
        public string SqlDateTimeCreated { get; set; }
        public string LastDayToServe { get; set; }
        public string JobId { get; set; }
        public string Court { get; set; }
        public string Plaintiff { get; set; }

        public string Plaintiffs { get; set; }
        public string Defendant { get; set; }
        public string Address { get; set; }
        public string? Status { get; set; }
        public string CaseNumber { get; set; }
        public string ClientReference { get; set; }
        public string TypeOfWrit { get; set; }
        public string ServiceType { get; set; }
        public string Date { get; set; }
        public string Time { get; set; }
        //public string InvoiceDue { get; set; }
        public string ClientStatus { get; set; }
        public string Zone { get; set; }
        public string LastServiceDate { get; set; }

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
    }

    public class InvoiceModel
    {
       public string Description { get; set; } 
        public int Quantity { get; set; }
        public decimal Rate { get; set; }
        public decimal Amount { get; set; }

    }

     public class PaymentModel
    {
        public DateTime Date { get; set; }
        public string Description { get; set; }
        public string Method { get; set; }
        public decimal Amount { get; set; }
        public string TimeOnly { get; internal set; }
    }

    public class CommentModel
{
    public string Date { get; set; }
    public string Time { get; set; }
    public string Body { get; set; }

    
        public bool Aff { get; set; }
        public bool FS { get; set; }
        public bool Att { get; set; }


    public string Source { get; set; }
}


    public class AttemptsModel
{
    public string Date { get; set; }
    public string Time { get; set; }
    public string Body { get; set; }

    
        public bool Aff { get; set; }
        public bool FS { get; set; }
        public bool Att { get; set; }


    public string Source { get; set; }
}

public class AttachmentModel
{
    public string Purpose { get; set; }
    public string Description { get; set; }
    public string Format { get; set; }
    public string FileExtension { get; set; }

    // Add FileData property to store file data as a byte array
    public byte[] FileData { get; set; }
     public string BlobMetadataId { get; set; }
}

}
