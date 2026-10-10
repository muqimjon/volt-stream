namespace VoltStream.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VoltStream.Domain.Entities;

public class BranchConfiguration : IEntityTypeConfiguration<Branch>
{
    public const long DefaultBranchId = 1;
    public const string DefaultBranchName = "Asosiy filial";

    public void Configure(EntityTypeBuilder<Branch> builder)
    {
        builder.ToTable("Branches");
        builder.HasKey(b => b.Id);

        builder.HasOne(b => b.Warehouse)
               .WithMany(w => w.Branches)
               .HasForeignKey(b => b.WarehouseId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany<Sale>()
               .WithOne(s => s.Branch)
               .HasForeignKey(s => s.BranchId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany<Supply>()
               .WithOne(s => s.Branch)
               .HasForeignKey(s => s.BranchId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany<User>()
               .WithOne(u => u.Branch)
               .HasForeignKey(u => u.BranchId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasData(new Branch
        {
            Id = DefaultBranchId,
            Name = DefaultBranchName,
            NormalizedName = DefaultBranchName.ToUpperInvariant(),
            IsActive = true,
            CreatedAt = DateTimeOffset.UnixEpoch,
            UpdatedAt = DateTimeOffset.UnixEpoch
        });
    }
}
