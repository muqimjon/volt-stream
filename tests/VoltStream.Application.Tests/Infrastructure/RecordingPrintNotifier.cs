namespace VoltStream.Application.Tests.Infrastructure;

using System.Collections.Concurrent;
using VoltStream.Application.Commons.Interfaces;
using VoltStream.Application.Features.PrintJobs.DTOs;

// SignalR o'rniga: agentlarga nima yuborilganini yozib boradi
public sealed class RecordingPrintNotifier : IPrintJobNotifier
{
    public ConcurrentQueue<PrintJobDto> Created { get; } = [];
    public ConcurrentQueue<PrintJobDto> Changed { get; } = [];

    public Task JobCreated(PrintJobDto job)
    {
        Created.Enqueue(job);

        return Task.CompletedTask;
    }

    public Task JobChanged(PrintJobDto job)
    {
        Changed.Enqueue(job);

        return Task.CompletedTask;
    }
}
