namespace VoltStream.Application.Commons.Interfaces;

using VoltStream.Application.Features.PrintJobs.DTOs;

public interface IPrintJobNotifier
{
    Task JobCreated(PrintJobDto job);
    Task JobChanged(PrintJobDto job);
}
