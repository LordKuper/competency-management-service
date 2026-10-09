namespace Competency.CareerFramework;

/// <summary>
/// The salary range of a career level; either bound may be absent, sent as null.
/// </summary>
internal sealed record CareerLevelSalaryRequest
{
    private const decimal MaxSalary = 9_999_999_999.99m;
    private const string AmountMessage = "Укажите сумму от 0 до 9 999 999 999,99";

    public required decimal? SalaryFrom { get; init; }

    public required decimal? SalaryTo { get; init; }

    /// <summary>
    /// Checks the range.
    /// </summary>
    /// <returns>The problems found by field name; empty when the request is valid.</returns>
    public Dictionary<string, string[]> Validate()
    {
        var errors = new Dictionary<string, string[]>();
        CheckAmount(errors, "salaryFrom", SalaryFrom);
        CheckAmount(errors, "salaryTo", SalaryTo);
        if (errors.Count == 0 && SalaryFrom > SalaryTo)
        {
            errors["salaryTo"] = ["«Оклад до» меньше «Оклад от»"];
        }

        return errors;
    }

    private static void CheckAmount(Dictionary<string, string[]> errors, string field, decimal? amount)
    {
        if (amount is < 0 or > MaxSalary)
        {
            errors[field] = [AmountMessage];
        }
    }
}
