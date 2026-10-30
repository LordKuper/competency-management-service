namespace Competency.Promotion;

/// <summary>
/// What a candidate learns of the outcome once the results are published: the final scores against the requirements and the decision. It holds no
/// rating, rank or answer of any one person.
/// </summary>
/// <param name="Decision">The decision of the chair; null when the commission stage ended with none.</param>
/// <param name="Competencies">Every competency the target level requires, in the order of the version.</param>
internal sealed record ApplicationResultResponse(PromotionDecision? Decision, IReadOnlyList<ResultCompetencyResponse> Competencies);
