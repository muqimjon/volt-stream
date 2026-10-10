namespace VoltStream.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VoltStream.Domain.Entities;

public class SaleDraftConfiguration : IEntityTypeConfiguration<SaleDraft>
{
    public void Configure(EntityTypeBuilder<SaleDraft> builder)
    {
        builder.ToTable("SaleDrafts");
        builder.HasKey(d => d.Id);

        builder.HasMany(d => d.Items)
               .WithOne(i => i.SaleDraft)
               .HasForeignKey(i => i.SaleDraftId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(d => d.Branch)
               .WithMany()
               .HasForeignKey(d => d.BranchId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(d => d.Customer)
               .WithMany()
               .HasForeignKey(d => d.CustomerId)
               .OnDelete(DeleteBehavior.Restrict);

        // Savdo tahrirlanganda o'chirib qayta yaratiladi; UpdateSaleCommand havolani yangi savdoga ko'chiradi.
        builder.HasOne(d => d.ConfirmedSale)
               .WithMany()
               .HasForeignKey(d => d.ConfirmedSaleId)
               .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(d => new { d.BranchId, d.Status });
    }
}
