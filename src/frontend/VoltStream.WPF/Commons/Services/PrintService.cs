namespace VoltStream.WPF.Commons.Services;

using System.Printing;
using System.Windows.Controls;
using System.Windows.Documents;

public static class PrintService
{
    public static Task<List<string>> GetPrinterNamesAsync() => Task.Run(() =>
    {
        try
        {
            using var server = new LocalPrintServer();
            return server
                .GetPrintQueues([EnumeratedPrintQueueTypes.Local, EnumeratedPrintQueueTypes.Connections])
                .Select(q => q.FullName)
                .OrderBy(n => n)
                .ToList();
        }
        catch
        {
            return [];
        }
    });

    public static void Print(FixedDocument document, string printerName, int copies, string title, string paper)
    {
        copies = Math.Max(1, copies);

        using var server = new LocalPrintServer();
        using var queue = server.GetPrintQueue(printerName);

        var ticket = queue.UserPrintTicket ?? queue.DefaultPrintTicket;
        ticket.CopyCount = copies;
        ticket.PageMediaSize = new PageMediaSize(paper == "A5" ? PageMediaSizeName.ISOA5 : PageMediaSizeName.ISOA4);
        ticket.PageOrientation = PageOrientation.Portrait;
        var validated = queue.MergeAndValidatePrintTicket(queue.UserPrintTicket, ticket).ValidatedPrintTicket;

        int passes = validated.CopyCount == copies ? 1 : copies;
        if (passes > 1)
            validated.CopyCount = 1;

        var dialog = new PrintDialog { PrintQueue = queue, PrintTicket = validated };
        for (int i = 0; i < passes; i++)
            dialog.PrintDocument(document.DocumentPaginator, title);
    }
}
