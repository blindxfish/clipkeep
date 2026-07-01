using ClipForge.Core.Classification;
using ClipForge.Core.Models;

namespace ClipForge.Tests;

public class ClassificationServiceTests
{
    private readonly ClassificationService _sut = new();

    [Theory]
    [InlineData("https://example.com")]
    [InlineData("www.example.com")]
    [InlineData("http://tgw-group.com/path?x=1")]
    public void Detects_urls(string text) =>
        Assert.Equal(ClipType.Url, _sut.Classify(text).Type);

    [Fact]
    public void Url_extracts_domain_tag()
    {
        var result = _sut.Classify("https://www.tgw-group.com/products");
        Assert.Equal(ClipType.Url, result.Type);
        Assert.Contains("tgw-group.com", result.Tags);
    }

    [Theory]
    [InlineData("john@example.com")]
    [InlineData("blindxfish@gmail.com")]
    public void Detects_email(string text) =>
        Assert.Equal(ClipType.Email, _sut.Classify(text).Type);

    [Theory]
    [InlineData("+31 6 12345678")]
    [InlineData("(555) 123-4567")]
    public void Detects_phone(string text) =>
        Assert.Equal(ClipType.Phone, _sut.Classify(text).Type);

    [Theory]
    [InlineData(@"C:\Projects\ClipForge")]
    [InlineData(@"\\server\share\file.pdf")]
    public void Detects_file_path(string text) =>
        Assert.Equal(ClipType.FilePath, _sut.Classify(text).Type);

    [Theory]
    [InlineData("#FFAA22")]
    [InlineData("rgb(255, 0, 0)")]
    public void Detects_color(string text) =>
        Assert.Equal(ClipType.Color, _sut.Classify(text).Type);

    [Fact]
    public void Detects_csharp()
    {
        var code = "using System;\npublic class Foo { }";
        var result = _sut.Classify(code);
        Assert.Equal(ClipType.Code, result.Type);
        Assert.Equal("csharp", result.CodeLanguage);
    }

    [Fact]
    public void Detects_sql()
    {
        var result = _sut.Classify("SELECT id, name FROM users WHERE active = 1");
        Assert.Equal(ClipType.Code, result.Type);
        Assert.Equal("sql", result.CodeLanguage);
    }

    [Fact]
    public void Detects_json() =>
        Assert.Equal("json", _sut.Classify("""{ "a": 1, "b": [2,3] }""").CodeLanguage);

    [Fact]
    public void Short_plain_text_is_text() =>
        Assert.Equal(ClipType.Text, _sut.Classify("just a note").Type);

    [Fact]
    public void Long_prose_is_long_text() =>
        Assert.Equal(ClipType.LongText, _sut.Classify(new string('a', 250)).Type);

    [Fact]
    public void Empty_is_text() =>
        Assert.Equal(ClipType.Text, _sut.Classify("   ").Type);
}
