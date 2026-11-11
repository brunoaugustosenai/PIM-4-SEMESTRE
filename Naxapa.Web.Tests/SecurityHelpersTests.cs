using Naxapa.Web.Security;
namespace Naxapa.Web.Tests;
public sealed class SecurityHelpersTests
{
    [Fact] public void TokensAreLongAndDifferent(){var a=SecurityHelpers.CreateOpaqueToken();var b=SecurityHelpers.CreateOpaqueToken();Assert.True(a.Length>=64);Assert.NotEqual(a,b);Assert.NotEqual(SecurityHelpers.HashToken(a),SecurityHelpers.HashToken(b));}
    [Theory][InlineData(" Bruno ","BRUNO")][InlineData("User@Email.Com","USER@EMAIL.COM")]public void IdentityIsNormalized(string input,string expected)=>Assert.Equal(expected,SecurityHelpers.NormalizeIdentity(input));
}
