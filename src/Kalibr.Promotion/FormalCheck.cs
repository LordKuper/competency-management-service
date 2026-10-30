using Competency.CareerFramework;
using Competency.OrgStructure;

namespace Competency.Promotion;

/// <summary>
/// The formal check of an application: the interval since hiring or the last transition, and the education. The one place that decides it, for
/// the HR entering the date, for the candidate filing and for the end of the check stage.
/// </summary>
internal static class FormalCheck
{
    /// <summary>
    /// Finds the conditions the application does not meet, which decide whether it goes on. A missing date fails the check; an interval the
    /// HR has chosen to ignore does not.
    /// </summary>
    /// <param name="application">The application.</param>
    /// <param name="requiredEducation">The education the target level requires; null when it requires none.</param>
    /// <param name="minIntervalMonths">The months that must pass between two transitions.</param>
    /// <param name="asOf">The day the interval is measured to: the day the wave publishes its results.</param>
    /// <returns>The conditions not met, in a fixed order; empty when the application passes.</returns>
    public static List<FormalFailure> Failures(PromotionApplication application, EducationLevel? requiredEducation, int minIntervalMonths, DateOnly asOf)
    {
        List<FormalFailure> failures = [];
        if (application.HiredOrPromotedOn is null)
        {
            failures.Add(FormalFailure.MissingDate);
        }

        failures.AddRange(Warnings(application, requiredEducation, minIntervalMonths, asOf)
            .Where(failure => failure != FormalFailure.IntervalNotPassed || !application.IgnoreInterval));
        return failures;
    }

    /// <summary>
    /// Finds the conditions that stand against the application now whatever the HR decides about the interval, and whether or not a date is entered yet.
    /// </summary>
    /// <param name="application">The application.</param>
    /// <param name="requiredEducation">The education the target level requires; null when it requires none.</param>
    /// <param name="minIntervalMonths">The months that must pass between two transitions.</param>
    /// <param name="asOf">The day the interval is measured to: the day the wave publishes its results.</param>
    /// <returns>The interval and education conditions not met; empty when both are.</returns>
    public static List<FormalFailure> Warnings(PromotionApplication application, EducationLevel? requiredEducation, int minIntervalMonths, DateOnly asOf)
    {
        List<FormalFailure> warnings = [];
        if (application.HiredOrPromotedOn is { } since && since.AddMonths(minIntervalMonths) > asOf)
        {
            warnings.Add(FormalFailure.IntervalNotPassed);
        }

        if (requiredEducation is { } required && application.Education < required)
        {
            warnings.Add(FormalFailure.EducationBelowRequired);
        }

        return warnings;
    }
}
