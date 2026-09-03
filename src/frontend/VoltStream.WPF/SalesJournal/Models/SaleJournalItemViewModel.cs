namespace VoltStream.WPF.SalesJournal.Models;

public class SaleJournalItemViewModel
{
    public long Id { get; set; }
    public DateTime Date { get; set; }
    public string? Customer { get; set; }
    public int RollCount { get; set; }
    public decimal Length { get; set; }
    public decimal Weight { get; set; }
    public decimal Amount { get; set; }
    public string? PrintStatus { get; set; }
}
