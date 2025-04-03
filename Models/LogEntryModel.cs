namespace CivilProcessERP.Models
{
    public class LogEntryModel
{
    public DateTime Date { get; set; }
    public string Time => Date.ToString("h:mm tt");
    public string Body { get; set; }

    public bool Aff { get; set; }
    public bool FS { get; set; }  // ✅ This must be writable

    public bool Att { get; set; }

    public string Source { get; set; }
}

public class InvoiceEntryModel
{
    public string Description { get; set; }
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
public string Description { get; set; }
public string Method { get; set; }
public DateTime Date { get; set; }

}




}