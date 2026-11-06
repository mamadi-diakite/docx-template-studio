namespace DocxTemplateStudio.Infrastructure.Persistence.Configurations;

internal sealed class TemplateConfiguration : IEntityTypeConfiguration<Template>
{
    public void Configure(EntityTypeBuilder<Template> builder)
    {
        builder.ToTable("Templates");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Module).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(t => t.Name).HasMaxLength(Template.NameMaxLength).IsRequired();
        builder.Property(t => t.FileName).HasMaxLength(Template.FileNameMaxLength).IsRequired();
        builder.Property(t => t.StorageKey).HasMaxLength(Template.StorageKeyMaxLength).IsRequired();
        builder.PrimitiveCollection(t => t.FieldKeys).IsRequired();

        builder.HasIndex(t => t.StorageKey).IsUnique();
        builder.HasIndex(t => t.Module);

        builder.HasOne(t => t.UploadedBy)
            .WithMany()
            .HasForeignKey(t => t.UploadedById)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
