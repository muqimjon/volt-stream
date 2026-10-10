namespace VoltStream.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VoltStream.Domain.Entities;

public class SaleDraftItemConfiguration : IEntityTypeConfiguration<SaleDraftItem>
{
    public void Configure(EntityTypeBuilder<SaleDraftItem> builder)
        => builder.HasOne(i => i.Product)
                  .WithMany()
                  .HasForeignKey(i => i.ProductId)
                  .OnDelete(DeleteBehavior.Restrict);
}
