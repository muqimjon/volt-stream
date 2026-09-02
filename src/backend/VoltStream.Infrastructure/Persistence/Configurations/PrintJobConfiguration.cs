namespace VoltStream.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VoltStream.Domain.Entities;

public class PrintJobConfiguration : IEntityTypeConfiguration<PrintJob>
{
    public void Configure(EntityTypeBuilder<PrintJob> builder)
    {
        builder.ToTable("PrintJobs");
        builder.HasKey(job => job.Id);

        builder.HasOne(job => job.Sale)
               .WithMany()
               .HasForeignKey(job => job.SaleId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(job => new { job.SaleId, job.Status });
        builder.HasIndex(job => new { job.BranchId, job.Status });
    }
}
