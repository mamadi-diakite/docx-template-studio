using DocxTemplateStudio.Application.Contracts.EmploymentContracts;
using DocxTemplateStudio.Domain;

namespace DocxTemplateStudio.Application.EmploymentContracts;

/// <summary>Checks the domain rules up front, so a form gets every error at once instead of the first exception.</summary>
public static class ContractValidator
{
    public static IReadOnlyDictionary<string, string[]> Validate(ContractInput input, out ContractType type)
    {
        ArgumentNullException.ThrowIfNull(input);

        var errors = new Dictionary<string, string[]>();
        CheckText(errors, "employeeFirstName", input.EmployeeFirstName, Contract.NameMaxLength);
        CheckText(errors, "employeeLastName", input.EmployeeLastName, Contract.NameMaxLength);
        CheckText(errors, "position", input.Position, Contract.PositionMaxLength);

        var typeName = Enum.GetNames<ContractType>()
            .FirstOrDefault(name => string.Equals(name, input.Type?.Trim(), StringComparison.OrdinalIgnoreCase));
        type = typeName is null ? default : Enum.Parse<ContractType>(typeName);
        if (typeName is null)
        {
            errors["type"] = ["Choose a contract type."];
        }
        else if (type == ContractType.Permanent && input.EndDate is not null)
        {
            errors["endDate"] = ["A permanent contract has no end date."];
        }
        else if (type != ContractType.Permanent && input.EndDate is null)
        {
            errors["endDate"] = ["This type of contract needs an end date."];
        }

        if (input.EndDate < input.StartDate)
        {
            errors["endDate"] = ["End date cannot be before the start date."];
        }

        if (input.GrossMonthlySalary < 0)
        {
            errors["grossMonthlySalary"] = ["Salary cannot be negative."];
        }

        return errors;
    }

    private static void CheckText(Dictionary<string, string[]> errors, string field, string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errors[field] = ["This field is required."];
        }
        else if (value.Trim().Length > maxLength)
        {
            errors[field] = [$"Use at most {maxLength} characters."];
        }
    }
}
