namespace DocxTemplateStudio.Domain;

public sealed class Contract
{
    public const int NameMaxLength = 100;
    public const int PositionMaxLength = 150;

    private Contract()
    {
    }

    public Contract(
        string employeeFirstName,
        string employeeLastName,
        string position,
        ContractType type,
        DateOnly startDate,
        DateOnly? endDate,
        decimal grossMonthlySalary,
        DateTime createdAtUtc)
    {
        Id = Guid.CreateVersion7();
        CreatedAtUtc = Guard.Utc(createdAtUtc, nameof(createdAtUtc));
        Apply(employeeFirstName, employeeLastName, position, type, startDate, endDate, grossMonthlySalary);
        UpdatedAtUtc = CreatedAtUtc;
    }

    public Guid Id { get; private set; }

    public string EmployeeFirstName { get; private set; } = string.Empty;

    public string EmployeeLastName { get; private set; } = string.Empty;

    public string Position { get; private set; } = string.Empty;

    public ContractType Type { get; private set; }

    public DateOnly StartDate { get; private set; }

    /// <summary>Required for every type except permanent contracts.</summary>
    public DateOnly? EndDate { get; private set; }

    public decimal GrossMonthlySalary { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime UpdatedAtUtc { get; private set; }

    public void Update(
        string employeeFirstName,
        string employeeLastName,
        string position,
        ContractType type,
        DateOnly startDate,
        DateOnly? endDate,
        decimal grossMonthlySalary,
        DateTime nowUtc)
    {
        var updatedAt = Guard.Utc(nowUtc, nameof(nowUtc));
        Apply(employeeFirstName, employeeLastName, position, type, startDate, endDate, grossMonthlySalary);
        UpdatedAtUtc = updatedAt;
    }

    private void Apply(
        string employeeFirstName,
        string employeeLastName,
        string position,
        ContractType type,
        DateOnly startDate,
        DateOnly? endDate,
        decimal grossMonthlySalary)
    {
        var firstName = Guard.Required(employeeFirstName, nameof(employeeFirstName), NameMaxLength);
        var lastName = Guard.Required(employeeLastName, nameof(employeeLastName), NameMaxLength);
        var positionTitle = Guard.Required(position, nameof(position), PositionMaxLength);
        var contractType = Guard.Defined(type, nameof(type));
        var salary = Guard.NotNegative(grossMonthlySalary, nameof(grossMonthlySalary));

        if (contractType == ContractType.Permanent && endDate is not null)
        {
            throw new ArgumentException("A permanent contract has no end date.", nameof(endDate));
        }

        if (contractType != ContractType.Permanent && endDate is null)
        {
            throw new ArgumentException("This type of contract needs an end date.", nameof(endDate));
        }

        if (endDate < startDate)
        {
            throw new ArgumentException("End date cannot be before start date.", nameof(endDate));
        }

        EmployeeFirstName = firstName;
        EmployeeLastName = lastName;
        Position = positionTitle;
        Type = contractType;
        StartDate = startDate;
        EndDate = endDate;
        GrossMonthlySalary = salary;
    }
}
