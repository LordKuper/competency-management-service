using AwesomeAssertions;
using Competency.OrgStructure.Import;
using Xunit;

namespace Competency.Tests;

/// <summary>
/// Which position names make an employee of the import file a candidate for the head of their unit: the name starts with a head word or phrase,
/// at a word boundary, in any letter case.
/// </summary>
public sealed class OrgImportHeadPositionTests
{
    [Theory]
    [InlineData("Руководитель отдела")]
    [InlineData("руководитель")]
    [InlineData("Директор по персоналу")]
    [InlineData("Генеральный директор")]
    [InlineData("ГЕНЕРАЛЬНЫЙ  ДИРЕКТОР")]
    [InlineData("Вице-президент по развитию")]
    [InlineData("Президент")]
    [InlineData("Начальник управления")]
    [InlineData("Заведующий складом")]
    [InlineData("Управляющий филиалом")]
    [InlineData("Глава представительства")]
    public void Ac30_APositionThatStartsWithAHeadWord_IsACandidate(string position)
    {
        ImportHeads.IsHeadPosition(position).Should().BeTrue();
    }

    [Theory]
    [InlineData("Заместитель директора")]
    [InlineData("Помощник руководителя")]
    [InlineData("Директорат")]
    [InlineData("Руководительница")]
    [InlineData("Инженер")]
    [InlineData("Главный инженер")]
    [InlineData("")]
    public void Ac30_AnyOtherPosition_IsNotACandidate(string position)
    {
        ImportHeads.IsHeadPosition(position).Should().BeFalse();
    }
}
