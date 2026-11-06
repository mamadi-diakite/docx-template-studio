namespace DocxTemplateStudio.Domain.Tests;

public sealed class ContractTests
{
    private static readonly DateOnly Start = new(2026, 12, 1);
    private static readonly DateOnly End = new(2027, 5, 31);

    [Fact]
    public void Creates_a_permanent_contract()
    {
        var contract = new Contract(" Anna ", " Smith ", " Data analyst ", ContractType.Permanent, Start, null, 3200m, TestDates.Now);

        Assert.NotEqual(Guid.Empty, contract.Id);
        Assert.Equal("Anna", contract.EmployeeFirstName);
        Assert.Equal("Smith", contract.EmployeeLastName);
        Assert.Equal("Data analyst", contract.Position);
        Assert.Equal(ContractType.Permanent, contract.Type);
        Assert.Equal(Start, contract.StartDate);
        Assert.Null(contract.EndDate);
        Assert.Equal(3200m, contract.GrossMonthlySalary);
        Assert.Equal(TestDates.Now, contract.CreatedAtUtc);
        Assert.Equal(TestDates.Now, contract.UpdatedAtUtc);
    }

    [Theory]
    [InlineData(ContractType.FixedTerm)]
    [InlineData(ContractType.Internship)]
    [InlineData(ContractType.Apprenticeship)]
    public void Fixed_length_contracts_keep_their_end_date(ContractType type)
    {
        var contract = new Contract("Anna", "Smith", "Analyst", type, Start, End, 1800m, TestDates.Now);

        Assert.Equal(End, contract.EndDate);
    }

    [Fact]
    public void A_contract_can_end_on_its_start_date()
    {
        var contract = new Contract("Anna", "Smith", "Analyst", ContractType.FixedTerm, Start, Start, 100m, TestDates.Now);

        Assert.Equal(Start, contract.EndDate);
    }

    [Fact]
    public void Permanent_contract_cannot_have_an_end_date()
    {
        var error = Assert.Throws<ArgumentException>(
            () => new Contract("Anna", "Smith", "Analyst", ContractType.Permanent, Start, End, 3200m, TestDates.Now));

        Assert.Equal("endDate", error.ParamName);
    }

    [Fact]
    public void Fixed_term_contract_needs_an_end_date()
    {
        var error = Assert.Throws<ArgumentException>(
            () => new Contract("Anna", "Smith", "Analyst", ContractType.FixedTerm, Start, null, 3200m, TestDates.Now));

        Assert.Equal("endDate", error.ParamName);
    }

    [Fact]
    public void End_date_cannot_be_before_start_date()
    {
        var error = Assert.Throws<ArgumentException>(
            () => new Contract("Anna", "Smith", "Analyst", ContractType.FixedTerm, Start, Start.AddDays(-1), 3200m, TestDates.Now));

        Assert.Equal("endDate", error.ParamName);
    }

    [Theory]
    [InlineData(" ", "Smith", "Analyst", "employeeFirstName")]
    [InlineData("Anna", " ", "Analyst", "employeeLastName")]
    [InlineData("Anna", "Smith", " ", "position")]
    public void Rejects_blank_text_fields(string firstName, string lastName, string position, string paramName)
    {
        var error = Assert.Throws<ArgumentException>(
            () => new Contract(firstName, lastName, position, ContractType.Permanent, Start, null, 3200m, TestDates.Now));

        Assert.Equal(paramName, error.ParamName);
    }

    [Fact]
    public void Rejects_unknown_type()
    {
        var error = Assert.Throws<ArgumentOutOfRangeException>(
            () => new Contract("Anna", "Smith", "Analyst", (ContractType)9, Start, null, 3200m, TestDates.Now));

        Assert.Equal("type", error.ParamName);
    }

    [Fact]
    public void Rejects_negative_salary()
    {
        var error = Assert.Throws<ArgumentOutOfRangeException>(
            () => new Contract("Anna", "Smith", "Analyst", ContractType.Permanent, Start, null, -1m, TestDates.Now));

        Assert.Equal("grossMonthlySalary", error.ParamName);
    }

    [Fact]
    public void Rejects_non_utc_creation_date()
    {
        var local = DateTime.SpecifyKind(TestDates.Now, DateTimeKind.Local);

        var error = Assert.Throws<ArgumentException>(
            () => new Contract("Anna", "Smith", "Analyst", ContractType.Permanent, Start, null, 3200m, local));

        Assert.Equal("createdAtUtc", error.ParamName);
    }

    [Fact]
    public void Update_replaces_the_details()
    {
        var contract = NewContract();
        var later = TestDates.Now.AddDays(1);

        contract.Update("Anna", "Brown", "Lead analyst", ContractType.FixedTerm, Start, End, 3500m, later);

        Assert.Equal("Brown", contract.EmployeeLastName);
        Assert.Equal("Lead analyst", contract.Position);
        Assert.Equal(ContractType.FixedTerm, contract.Type);
        Assert.Equal(End, contract.EndDate);
        Assert.Equal(3500m, contract.GrossMonthlySalary);
        Assert.Equal(TestDates.Now, contract.CreatedAtUtc);
        Assert.Equal(later, contract.UpdatedAtUtc);
    }

    [Fact]
    public void Failed_update_keeps_the_previous_details()
    {
        var contract = NewContract();

        Assert.Throws<ArgumentException>(
            () => contract.Update("Anna", "Brown", "Lead", ContractType.FixedTerm, Start, null, 3500m, TestDates.Now.AddDays(1)));

        Assert.Equal("Smith", contract.EmployeeLastName);
        Assert.Equal(ContractType.Permanent, contract.Type);
        Assert.Equal(TestDates.Now, contract.UpdatedAtUtc);
    }

    [Fact]
    public void Update_rejects_non_utc_date()
    {
        var contract = NewContract();

        var error = Assert.Throws<ArgumentException>(() => contract.Update(
            "Anna", "Smith", "Analyst", ContractType.Permanent, Start, null, 3200m,
            DateTime.SpecifyKind(TestDates.Now, DateTimeKind.Local)));

        Assert.Equal("nowUtc", error.ParamName);
    }

    private static Contract NewContract() =>
        new("Anna", "Smith", "Analyst", ContractType.Permanent, Start, null, 3200m, TestDates.Now);
}
