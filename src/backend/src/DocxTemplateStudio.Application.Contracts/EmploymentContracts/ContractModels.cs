namespace DocxTemplateStudio.Application.Contracts.EmploymentContracts;

/// <param name="Type">Permanent, FixedTerm, Internship or Apprenticeship.</param>
/// <param name="EndDate">Empty for permanent contracts, required for the other types.</param>
public sealed record ContractInput(
    string EmployeeFirstName,
    string EmployeeLastName,
    string Position,
    string Type,
    DateOnly StartDate,
    DateOnly? EndDate,
    decimal GrossMonthlySalary);

public sealed record ContractResponse(
    Guid Id,
    string EmployeeFirstName,
    string EmployeeLastName,
    string Position,
    string Type,
    DateOnly StartDate,
    DateOnly? EndDate,
    decimal GrossMonthlySalary,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);
