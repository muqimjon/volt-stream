namespace VoltStream.WebApi.Hubs;

using Microsoft.AspNetCore.SignalR;
using VoltStream.Application.Commons.Interfaces;
using VoltStream.Application.Features.PrintJobs.DTOs;

public class PrintJobNotifier(IHubContext<PrintHub> hub) : IPrintJobNotifier
{
    public Task JobCreated(PrintJobDto job)
        => Task.WhenAll(
            hub.Clients.Group($"branch-{job.BranchId}").SendAsync("PrintJobCreated", job),
            hub.Clients.Group($"user-{job.RequestedByUserId}").SendAsync("PrintJobStatusChanged", job));

    public Task JobChanged(PrintJobDto job)
        => Task.WhenAll(
            hub.Clients.Group($"branch-{job.BranchId}").SendAsync("PrintJobStatusChanged", job),
            hub.Clients.Group($"user-{job.RequestedByUserId}").SendAsync("PrintJobStatusChanged", job));
}
