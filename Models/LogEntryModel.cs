using static System.Runtime.InteropServices.JavaScript.JSType;

namespace CivilProcessERP.Models
{
    public class LogEntryModel
{
    public DateTime Date { get; set; }
    public string Time => Date.ToShortTimeString();

    public string Body { get; set; } = string.Empty;

    public bool Aff { get; set; }
    public bool FS { get; set; }  // ✅ This must be writable

    public bool Att { get; set; }

    public string Source { get; set; } = string.Empty;
}

public class InvoiceEntryModel
{
    public string Description { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal Rate { get; set; }
    private decimal _amount;

public decimal Amount
{
    get => _amount;
    set => _amount = value;
}
 // Optional if auto-calculate
}

public class PaymentEntryModel
{
   public decimal Amount { get; set; }
public string Description { get; set; } = string.Empty;
public string Method { get; set; } = string.Empty;
public DateTime Date { get; set; }

public DateTime DateOnly
{
    get => Date.Date;
    set => Date = new DateTime(value.Year, value.Month, value.Day, Date.Hour, Date.Minute, Date.Second);
}

public TimeSpan Time { get; set; }

}




}