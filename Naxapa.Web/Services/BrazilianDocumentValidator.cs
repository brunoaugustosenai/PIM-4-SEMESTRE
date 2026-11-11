namespace Naxapa.Web.Services;

public static class BrazilianDocumentValidator
{
    public static string DigitsOnly(string? value)
        => new string((value ?? string.Empty).Where(char.IsDigit).ToArray());

    public static bool IsValidCpfOrCnpj(string? value)
    {
        var digits = DigitsOnly(value);
        return digits.Length switch
        {
            11 => IsValidCpf(digits),
            14 => IsValidCnpj(digits),
            _ => false
        };
    }

    public static bool IsValidCpf(string? value)
    {
        var cpf = DigitsOnly(value);
        if (cpf.Length != 11 || cpf.Distinct().Count() == 1)
            return false;

        var sum = 0;
        for (var i = 0; i < 9; i++) sum += (cpf[i] - '0') * (10 - i);
        var first = 11 - (sum % 11);
        if (first >= 10) first = 0;
        if (cpf[9] - '0' != first) return false;

        sum = 0;
        for (var i = 0; i < 10; i++) sum += (cpf[i] - '0') * (11 - i);
        var second = 11 - (sum % 11);
        if (second >= 10) second = 0;
        return cpf[10] - '0' == second;
    }

    public static bool IsValidCnpj(string? value)
    {
        var cnpj = DigitsOnly(value);
        if (cnpj.Length != 14 || cnpj.Distinct().Count() == 1)
            return false;

        int Calculate(int length, int[] weights)
        {
            var sum = 0;
            for (var i = 0; i < length; i++) sum += (cnpj[i] - '0') * weights[i];
            var remainder = sum % 11;
            return remainder < 2 ? 0 : 11 - remainder;
        }

        var first = Calculate(12, [5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2]);
        if (cnpj[12] - '0' != first) return false;
        var second = Calculate(13, [6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2]);
        return cnpj[13] - '0' == second;
    }
}
