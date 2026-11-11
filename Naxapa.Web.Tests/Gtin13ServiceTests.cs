using Naxapa.Web.Services;

namespace Naxapa.Web.Tests;

public sealed class Gtin13ServiceTests
{
    [Fact]
    public void Generate_ReturnsValidThirteenDigitCode()
    {
        var service = new Gtin13Service();
        for (var i = 0; i < 100; i++)
        {
            var value = service.Generate();
            Assert.Equal(13, value.Length);
            Assert.All(value, c => Assert.True(char.IsDigit(c)));
            Assert.StartsWith("2", value);
            Assert.True(service.IsValid(value));
        }
    }

    [Theory]
    [InlineData("7894900011517", true)]
    [InlineData("7894900011518", false)]
    [InlineData("123", false)]
    public void IsValid_ValidatesCheckDigit(string value, bool expected)
    {
        Assert.Equal(expected, new Gtin13Service().IsValid(value));
    }
}
