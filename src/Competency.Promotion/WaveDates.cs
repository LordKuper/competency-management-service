namespace Competency.Promotion;

/// <summary>
/// The rules for the dates of a wave: a date never falls on a day before today or before the date of the previous stage; once the wave has
/// started its start date is fixed, and so is the end date of every stage that has finished.
/// </summary>
internal static class WaveDates
{
    private const int HorizonYears = 5;
    private const string EndsField = "endsOn";
    private const string StartField = "startsOn";
    private const string BeforeTodayMessage = "Дата не раньше сегодняшнего дня";
    private const string BeforePreviousMessage = "Дата не раньше даты завершения предыдущего этапа";
    private const string FinishedMessage = "Этап завершён: его дату нельзя изменить";

    /// <summary>
    /// Checks the dates of a request against the rules.
    /// </summary>
    /// <param name="request">The request.</param>
    /// <param name="wave">The wave being changed; null for a wave being announced.</param>
    /// <param name="stage">The stage the wave is in, as <see cref="PromotionCalendar.StageOf"/> finds it; 0 for a wave being announced.</param>
    /// <param name="today">The current day.</param>
    /// <returns>The problems found by field name, an end date by <c>endsOn[n]</c> with n counting from 0; empty when the dates are valid.</returns>
    public static Dictionary<string, string[]> Validate(WaveRequest request, PromotionWave? wave, int stage, DateOnly today)
    {
        var errors = new Dictionary<string, string[]>();
        if (request.EndsOn is not { Length: PromotionWave.StageCount })
        {
            errors[EndsField] = [$"Укажите дату завершения каждого из {PromotionWave.StageCount} этапов"];
            return errors;
        }

        var horizon = today.AddYears(HorizonYears);
        if (wave is not null && stage >= 1)
        {
            if (request.StartsOn != wave.StartsOn)
            {
                errors[StartField] = ["Дату начала нельзя изменить после начала волны"];
            }
        }
        else if (request.StartsOn < today || request.StartsOn > horizon)
        {
            errors[StartField] = [request.StartsOn < today ? BeforeTodayMessage : HorizonMessage()];
        }

        var previous = request.StartsOn;
        for (var index = 0; index < PromotionWave.StageCount; index++)
        {
            var date = request.EndsOn[index];
            var problem = wave is not null && index + 1 < stage
                ? date == wave.EndsOn[index] ? null : FinishedMessage
                : EndProblem(date, previous, today, horizon);
            if (problem is not null)
            {
                errors[$"{EndsField}[{index}]"] = [problem];
            }

            previous = date;
        }

        return errors;
    }

    private static string? EndProblem(DateOnly date, DateOnly previous, DateOnly today, DateOnly horizon)
    {
        if (date < today)
        {
            return BeforeTodayMessage;
        }

        if (date < previous)
        {
            return BeforePreviousMessage;
        }

        return date > horizon ? HorizonMessage() : null;
    }

    private static string HorizonMessage() => $"Дата не позже чем через {HorizonYears} лет";
}
