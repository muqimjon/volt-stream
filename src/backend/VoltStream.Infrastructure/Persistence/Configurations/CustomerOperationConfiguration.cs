namespace VoltStream.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VoltStream.Domain.Entities;

public class CustomerOperationConfiguration : IEntityTypeConfiguration<CustomerOperation>
{
    public void Configure(EntityTypeBuilder<CustomerOperation> builder)
    {
        builder.ToTable("CustomerOperations");
        builder.HasKey(co => co.Id);

        builder.HasOne(co => co.Account)
               .WithMany()
               .HasForeignKey(co => co.AccountId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}