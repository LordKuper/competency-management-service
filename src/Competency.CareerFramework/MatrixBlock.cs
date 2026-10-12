namespace Competency.CareerFramework;

/// <summary>
/// A named block of competencies, such as general or managerial ones; a mandatory block may set its own readiness threshold.
/// The identifier is chosen by the client and only needs to be unique in the document.
/// </summary>
internal sealed record MatrixBlock(Guid Id, string Name, bool IsMandatory);
