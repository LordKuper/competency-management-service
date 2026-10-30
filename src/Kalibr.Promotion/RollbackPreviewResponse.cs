namespace Kalibr.Promotion;

/// <summary>
/// What going back to a stage would reset or compute again, for the user to confirm.
/// </summary>
/// <param name="Stage">The stage the wave would go back to.</param>
/// <param name="Effects">The consequences for applications, only those that touch at least one.</param>
/// <param name="LetterStages">The stages whose end letters were sent and would be sent again when those stages end anew.</param>
/// <param name="RaterReminderAgain">Whether the reminder to the raters, already sent, would be sent again.</param>
internal sealed record RollbackPreviewResponse(
    int Stage,
    IReadOnlyList<RollbackEffect> Effects,
    IReadOnlyList<int> LetterStages,
    bool RaterReminderAgain);
