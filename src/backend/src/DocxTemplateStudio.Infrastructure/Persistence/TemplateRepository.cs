namespace DocxTemplateStudio.Infrastructure.Persistence;

public sealed class TemplateRepository(DocxTemplateStudioDbContext db) : ITemplateRepository
{
    public async Task<IReadOnlyList<Template>> ListAsync(DocumentModule? module, CancellationToken cancellationToken)
    {
        var query = db.Templates.AsNoTracking();
        if (module is { } filter)
        {
            query = query.Where(t => t.Module == filter);
        }

        var templates = await query.ToListAsync(cancellationToken);

        return
        [
            .. templates
                .OrderBy(t => t.Module)
                .ThenBy(t => t.Name, StringComparer.InvariantCultureIgnoreCase)
                .ThenBy(t => t.UploadedAtUtc),
        ];
    }

    public Task<Template?> FindAsync(Guid id, CancellationToken cancellationToken) =>
        db.Templates.SingleOrDefaultAsync(t => t.Id == id, cancellationToken);

    public void Add(Template template) => db.Templates.Add(template);

    public void Remove(Template template) => db.Templates.Remove(template);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => db.SaveChangesAsync(cancellationToken);
}
