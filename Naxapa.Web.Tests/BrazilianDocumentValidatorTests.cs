using Naxapa.Web.Services;

namespace Naxapa.Web.Tests;

public sealed class BrazilianDocumentValidatorTests
{
    [Theory]
    [InlineData("529.982.247-25", true)]
    [InlineData("52998224724", false)]
    [InlineData("11.222.333/0001-81", true)]
    [InlineData("11222333000180", false)]
    [InlineData("111.111.111-11", false)]
    public void IsValidCpfOrCnpj_Works(string document, bool expected)
    {
        Assert.Equal(expected, BrazilianDocumentValidator.IsValidCpfOrCnpj(document));
    }
}
