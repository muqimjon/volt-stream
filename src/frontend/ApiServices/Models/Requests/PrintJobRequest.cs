namespace ApiServices.Models.Requests;

public record PrintJobRequest
{
    public long SaleId { get; set; }
    public int Copies { get; set; } = 1;
}

public record PrintJobClaimRequest
{
    public string PrinterName { get; set; } = string.Empty;
}

public record PrintJobFailRequest
{
    public string Error { get; set; } = string.Empty;
}

public record PrintJobRejectRequest
{
    public string Reason { get; set; } = string.Empty;
}
