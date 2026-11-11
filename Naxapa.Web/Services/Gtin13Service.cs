using System.Security.Cryptography;

namespace Naxapa.Web.Services;

public sealed class Gtin13Service
{
    public string Generate()
    {
        // Prefixo 2 é apropriado para numeração interna/restrita. O código gerado
        // possui dígito verificador GTIN-13 válido, mas não substitui um prefixo GS1 licenciado.
        Span<int> digits = stackalloc int[13];
        digits[0] = 2;
        for (var i = 1; i < 12; i++)
            digits[i] = RandomNumberGenerator.GetInt32(0, 10);

        digits[12] = CalculateCheckDigit(digits[..12]);
        return string.Concat(digits.ToArray());
    }

    public bool IsValid(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length != 13 || value.Any(c => !char.IsDigit(c)))
            return false;

        Span<int> digits = stackalloc int[13];
        for (var i = 0; i < value.Length; i++)
            digits[i] = value[i] - '0';

        return CalculateCheckDigit(digits[..12]) == digits[12];
    }

    public static int CalculateCheckDigit(ReadOnlySpan<int> firstTwelveDigits)
    {
        if (firstTwelveDigits.Length != 12)
            throw new ArgumentException("GTIN-13 requer exatamente 12 dígitos antes do verificador.", nameof(firstTwelveDigits));

        var sum = 0;
        for (var i = 0; i < firstTwelveDigits.Length; i++)
            sum += firstTwelveDigits[i] * (i % 2 == 0 ? 1 : 3);

        return (10 - (sum % 10)) % 10;
    }
}
