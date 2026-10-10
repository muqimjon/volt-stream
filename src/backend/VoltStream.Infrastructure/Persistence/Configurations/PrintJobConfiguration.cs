namespace VoltStream.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VoltStream.Domain.Entities;

public class PrintJobConfiguration : IEntityTypeConfiguration<PrintJob>
{
    public void Configure(EntityTypeBuilder<PrintJob> builder)
    {
        builder.ToTable("PrintJobs", table => table.HasCheckConstraint(
            "CK_PrintJobs_SaleOrDraft", "(\"SaleId\" IS NULL) <> (\"SaleDraftId\" IS NULL)"));
        builder.HasKey(job => job.Id);

        builder.HasOne(job => job.Sale)
               .WithMany()
               .HasForeignKey(job => job.SaleId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(job => job.SaleDraft)
               .WithMany()
               .HasForeignKey(job => job.SaleDraftId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(job => new { job.SaleId, job.Status });
        builder.HasIndex(job => new { job.SaleDraftId, job.Status });
        builder.HasIndex(job => new { job.BranchId, job.Status });
    }
}
