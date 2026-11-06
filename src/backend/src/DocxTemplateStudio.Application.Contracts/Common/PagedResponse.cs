namespace DocxTemplateStudio.Application.Contracts.Common;

/// <param name="Page">1-based page number.</param>
public sealed record PagedResponse<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount);
