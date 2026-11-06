namespace DocxTemplateStudio.Application.Tests;

public sealed class ContractValidatorTests
{
    private static readonly DateOnly Start = new(2026, 9, 1);

    private static readonly ContractInput Valid =
        new("Marion", "Aubert", "Payroll officer", "Permanent", Start, null, 2650m);

    [Theory]
    [InlineData("Permanent", ContractType.Permanent)]
    [InlineData(" fixedterm ", ContractType.FixedTerm)]
    public void Accepts_a_valid_contract_and_reads_the_type(string type, ContractType expected)
    {
        var endDate = expected == ContractType.Permanent ? (DateOnly?)null : Start.AddMonths(6);

        var errors = ContractValidator.Validate(Valid with { Type = type, EndDate = endDate }, out var parsed);

        Assert.Empty(errors);
        Assert.Equal(expected, parsed);
    }

    [Theory]
    [InlineData("", "employeeFirstName", "This field is required.")]
    [InlineData("long", "employeeFirstName", "Use at most 100 characters.")]
    public void Checks_required_text_and_length(string firstName, string field, string message)
    {
        firstName = firstName == "long" ? new string('a', Contract.NameMaxLength + 1) : firstName;

        var errors = ContractValidator.Validate(Valid with { EmployeeFirstName = firstName }, out _);

        Assert.Equal([message], errors[field]);
    }

    [Fact]
    public void Reports_every_text_field()
    {
        var errors = ContractValidator.Validate(Valid with { EmployeeFirstName = " ", EmployeeLastName = "", Position = null! }, out _);

        Assert.Equal(["employeeFirstName", "employeeLastName", "position"], errors.Keys.Order());
    }

    [Theory]
    [InlineData("Freelance")]
    [InlineData("1")]
    [InlineData(null)]
    public void Rejects_an_unknown_type(string? type)
    {
        var errors = ContractValidator.Validate(Valid with { Type = type! }, out _);

        Assert.Equal(["Choose a contract type."], errors["type"]);
    }

    [Fact]
    public void Permanent_contract_has_no_end_date()
    {
        var errors = ContractValidator.Validate(Valid with { EndDate = Start.AddYears(1) }, out _);

        Assert.Equal(["A permanent contract has no end date."], errors["endDate"]);
    }

    [Fact]
    public void Other_types_need_an_end_date()
    {
        var errors = ContractValidator.Validate(Valid with { Type = "Internship" }, out _);

        Assert.Equal(["This type of contract needs an end date."], errors["endDate"]);
    }

    [Fact]
    public void End_date_cannot_be_before_start_date()
    {
        var errors = ContractValidator.Validate(Valid with { Type = "FixedTerm", EndDate = Start.AddDays(-1) }, out _);

        Assert.Equal(["End date cannot be before the start date."], errors["endDate"]);
    }

    [Fact]
    public void Salary_cannot_be_negative()
    {
        var errors = ContractValidator.Validate(Valid with { GrossMonthlySalary = -1m }, out _);

        Assert.Equal(["Salary cannot be negative."], errors["grossMonthlySalary"]);
    }

    [Fact]
    public void Rejects_a_missing_input()
    {
        Assert.Throws<ArgumentNullException>(() => ContractValidator.Validate(null!, out _));
    }
}
