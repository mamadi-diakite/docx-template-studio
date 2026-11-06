using DocxTemplateStudio.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DocxTemplateStudio.Infrastructure.Persistence.Configurations;

internal sealed class ContractConfiguration : IEntityTypeConfiguration<Contract>
{
    public void Configure(EntityTypeBuilder<Contract> builder)
    {
        builder.ToTable("Contracts");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.EmployeeFirstName).HasMaxLength(Contract.NameMaxLength).IsRequired();
        builder.Property(c => c.EmployeeLastName).HasMaxLength(Contract.NameMaxLength).IsRequired();
        builder.Property(c => c.Position).HasMaxLength(Contract.PositionMaxLength).IsRequired();
        builder.Property(c => c.Type).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(c => c.GrossMonthlySalary).HasPrecision(12, 2);

        builder.HasIndex(c => new { c.EmployeeLastName, c.EmployeeFirstName });
    }
}
