using ClipForge.Core.Security;

namespace ClipForge.Tests;

public class SensitiveContentDetectorTests
{
    private readonly SensitiveContentDetector _sut = new();

    [Theory]
    [InlineData("4111 1111 1111 1111")]                                   // Visa test number (Luhn-valid)
    [InlineData("-----BEGIN RSA PRIVATE KEY-----")]
    [InlineData("eyJhbGciOiJIUzI1NiJ9.eyJzdWIiOiIxIn0.abc123DEF456ghi")]  // JWT
    [InlineData("sk-abcdefghijklmnopqrstuvwxyz0123")]                     // API key
    [InlineData("api_key = 1234567890abcdefghijklmno")]
    public void Flags_sensitive(string text) =>
        Assert.True(_sut.IsSensitive(text));

    [Theory]
    [InlineData("just a normal sentence")]
    [InlineData("1234")]
    [InlineData("https://example.com")]
    public void Allows_ordinary(string text) =>
        Assert.False(_sut.IsSensitive(text));

    [Fact]
    public void Rejects_invalid_luhn_number() =>
        Assert.False(_sut.IsSensitive("1234 5678 9012 3456"));
}
