namespace DocxTemplateStudio.Application.Templates;

public interface ITemplateRepository
{
    /// <returns>Templates sorted by module, then name, then upload date.</returns>
    Task<IReadOnlyList<Template>> ListAsync(DocumentModule? module, CancellationToken cancellationToken);

    Task<Template?> FindAsync(Guid id, CancellationToken cancellationToken);

    void Add(Template template);

    void Remove(Template template);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
