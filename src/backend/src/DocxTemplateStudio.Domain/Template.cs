namespace DocxTemplateStudio.Domain;

public sealed class Template
{
    public const int NameMaxLength = 150;
    public const int FileNameMaxLength = 255;
    public const int StorageKeyMaxLength = 200;
    public const int FieldKeyMaxLength = 100;
    public const string FileExtension = ".docx";

    private Template()
    {
    }

    public Template(
        DocumentModule module,
        string name,
        string fileName,
        string storageKey,
        long sizeBytes,
        IEnumerable<string> fieldKeys,
        Guid uploadedById,
        DateTime uploadedAtUtc)
    {
        ArgumentNullException.ThrowIfNull(fieldKeys);

        Id = Guid.CreateVersion7();
        Module = Guard.Defined(module, nameof(module));
        Name = Guard.Required(name, nameof(name), NameMaxLength);
        FileName = Guard.Required(fileName, nameof(fileName), FileNameMaxLength);
        StorageKey = Guard.Required(storageKey, nameof(storageKey), StorageKeyMaxLength);
        SizeBytes = Guard.Positive(sizeBytes, nameof(sizeBytes));
        FieldKeys = fieldKeys
            .Select(key => Guard.Required(key, nameof(fieldKeys), FieldKeyMaxLength))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        UploadedById = Guard.NotEmpty(uploadedById, nameof(uploadedById));
        UploadedAtUtc = Guard.Utc(uploadedAtUtc, nameof(uploadedAtUtc));

        if (!FileName.EndsWith(FileExtension, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Templates must be .docx files.", nameof(fileName));
        }
    }

    public Guid Id { get; private set; }

    public DocumentModule Module { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string FileName { get; private set; } = string.Empty;

    /// <summary>Key of the file in the storage, never shown to users.</summary>
    public string StorageKey { get; private set; } = string.Empty;

    public long SizeBytes { get; private set; }

    /// <summary>Tags of the content controls found in the template, in document order, without duplicates.</summary>
    public string[] FieldKeys { get; private set; } = [];

    public Guid UploadedById { get; private set; }

    public User? UploadedBy { get; private set; }

    public DateTime UploadedAtUtc { get; private set; }
}
