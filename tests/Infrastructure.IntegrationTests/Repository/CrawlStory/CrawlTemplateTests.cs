using SurveillanceCameras.Application.Common.Exceptions;
using SurveillanceCameras.Infrastructure.Repository.CrawlStory.Configurable;

namespace SurveillanceCameras.Infrastructure.IntegrationTests.Repository.CrawlStory;

public class CrawlTemplateTests
{
    private static readonly Dictionary<string, string> Vars = new(StringComparer.OrdinalIgnoreCase)
    {
        ["url"] = "https://site.test/truyen/a",
        ["key"] = "abcdef",
        ["offset"] = "50",
        ["size"] = "50",
        ["n"] = "2"
    };

    [Test]
    public void Render_ReplacesVariablesAndKeepsLiteralText()
        => Assert.That(CrawlTemplate.Render("{url}/trang-{offset}", Vars), Is.EqualTo("https://site.test/truyen/a/trang-50"));

    [Test]
    public void Evaluate_RotateMatchesJsSubstring()
        // JS: text.substring(2) + text.substring(0, 2)
        => Assert.That(CrawlTemplate.Evaluate("rotate(key + offset, n)", Vars), Is.EqualTo("cdef50ab"));

    [Test]
    public void Evaluate_Sha256OfConcatenation()
        => Assert.That(CrawlTemplate.Evaluate("sha256('abc')", Vars),
            Is.EqualTo("ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad"));

    [Test]
    public void Render_UnknownVariable_Throws()
        => Assert.Throws<CrawlException>(() => CrawlTemplate.Render("{missing}", Vars));

    [Test]
    public void Render_BraceInsideStringLiteral_IsNotTreatedAsClose()
        => Assert.That(CrawlTemplate.Render("{'a}b' + offset}", Vars), Is.EqualTo("a}b50"));
}
