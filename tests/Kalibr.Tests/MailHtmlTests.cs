using AwesomeAssertions;
using Kalibr.Platform;
using Kalibr.Tests.Infrastructure;
using Xunit;

namespace Kalibr.Tests;

/// <summary>
/// Every letter goes out as text with an HTML twin built by one rule: paragraphs, clickable links named by the line above them, and no markup
/// from the names and comments the system puts in.
/// </summary>
public sealed class MailHtmlTests(TestEnvironment environment)
{
    private const string Link = "https://calibr.test.local/apply";

    [Fact]
    public void Ac87_BlocksBecomeParagraphs_AndLinesInsideABlockAreBrokenWithBr()
    {
        var html = MailSender.ToHtml("Здравствуйте.\r\nВам письмо.\n\nВторой абзац.");

        html.Should().Contain("<p>Здравствуйте.<br>Вам письмо.</p><p>Второй абзац.</p>");
    }

    [Fact]
    public void Ac87_AnAddressLine_BecomesALinkNamedByTheLineAboveIt_WhichIsNotShownTwice()
    {
        var html = MailSender.ToHtml($"Откройте ссылку.\n\nПодать заявку:\n{Link}");

        html.Should().Contain($"<p><a href=\"{Link}\">Подать заявку</a></p>");
        html.Should().NotContain("Подать заявку:");
    }

    [Fact]
    public void Ac87_AnAddressWithNoLineAboveItInItsBlock_IsNamedByItself()
    {
        MailSender.ToHtml($"Текст.\n\n{Link}").Should().Contain($"<a href=\"{Link}\">{Link}</a>");
    }

    [Fact]
    public void Ac87_NamesAndCommentsFromTheSystem_AreShownAsTextAndNeverBecomeMarkup()
    {
        var html = MailSender.ToHtml("Комментарий: <script>alert(1)</script> & \"кавычки\"\n\nОткрыть:\nhttps://calibr.test.local/a?x=1&y=2");

        html.Should().NotContain("<script>");
        html.Should().Contain("&lt;script&gt;alert(1)&lt;/script&gt; &amp; &quot;кавычки&quot;");
        html.Should().Contain("href=\"https://calibr.test.local/a?x=1&amp;y=2\"");
    }

    [Fact]
    public void Ac87_ALineThatIsNotASingleWebAddress_IsNotALink()
    {
        var html = MailSender.ToHtml("Адрес https://calibr.test.local/apply в тексте\n\nftp://host/file");

        html.Should().NotContain("<a ");
    }

    [Fact]
    public async Task Ac87_ALetterReachesTheMailboxAsTextAndAsHtml_WithAClickableLinkNamedByTheAction()
    {
        var host = await environment.SharedHostAsync();
        var admin = await host.AdminAsync();
        var invited = await admin.InviteUserAsync();

        var mail = (await environment.Mail.WaitForAsync(invited.Email)).Should().ContainSingle().Subject;

        mail.Text.Should().Contain(mail.Link.ToString());
        mail.Html.Should().Contain($"<a href=\"{mail.Link}\">Задать пароль</a>");
    }
}
