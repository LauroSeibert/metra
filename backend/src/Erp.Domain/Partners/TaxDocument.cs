namespace Erp.Domain.Partners;

public enum TaxDocumentKind
{
    Cpf = 1,
    Cnpj = 2,
}

/// <summary>
/// CPF (11 dígitos) e CNPJ (14 posições). Desde julho de 2026 o CNPJ pode ser alfanumérico: as 12
/// primeiras posições aceitam letras maiúsculas e as 2 últimas são dígitos verificadores, calculados
/// com o valor ASCII − 48 de cada posição (para dígitos, o próprio dígito).
/// </summary>
public static class TaxDocument
{
    /// <summary>Remove pontuação e espaços e põe letras em maiúsculas ("12.ABC.345/01DE-35" → "12ABC34501DE35").</summary>
    public static string Normalize(string document)
    {
        ArgumentNullException.ThrowIfNull(document);
        return new string([.. document.Where(char.IsAsciiLetterOrDigit).Select(char.ToUpperInvariant)]);
    }

    /// <summary>Tipo do documento já normalizado, ou nulo se o tamanho não for de CPF nem de CNPJ.</summary>
    public static TaxDocumentKind? KindOf(string normalized) => normalized?.Length switch
    {
        11 => TaxDocumentKind.Cpf,
        14 => TaxDocumentKind.Cnpj,
        _ => null,
    };

    public static bool IsValid(string normalized) => KindOf(normalized) switch
    {
        TaxDocumentKind.Cpf => IsValidCpf(normalized),
        TaxDocumentKind.Cnpj => IsValidCnpj(normalized),
        _ => false,
    };

    /// <summary>Formata para exibição: 000.000.000-00 ou 00.000.000/0000-00.</summary>
    public static string Format(string normalized) => KindOf(normalized) switch
    {
        TaxDocumentKind.Cpf => $"{normalized[..3]}.{normalized[3..6]}.{normalized[6..9]}-{normalized[9..]}",
        TaxDocumentKind.Cnpj => $"{normalized[..2]}.{normalized[2..5]}.{normalized[5..8]}/{normalized[8..12]}-{normalized[12..]}",
        _ => normalized,
    };

    public static bool IsValidCpf(string cpf)
    {
        if (cpf is not { Length: 11 } || !cpf.All(char.IsAsciiDigit) || cpf.Distinct().Count() == 1)
        {
            return false;
        }

        return CpfCheckDigit(cpf.AsSpan(0, 9)) == cpf[9] - '0'
            && CpfCheckDigit(cpf.AsSpan(0, 10)) == cpf[10] - '0';
    }

    public static bool IsValidCnpj(string cnpj)
    {
        if (cnpj is not { Length: 14 }
            || !cnpj.AsSpan(0, 12).ToArray().All(c => char.IsAsciiDigit(c) || char.IsAsciiLetterUpper(c))
            || !char.IsAsciiDigit(cnpj[12])
            || !char.IsAsciiDigit(cnpj[13])
            || cnpj.Distinct().Count() == 1)
        {
            return false;
        }

        return CnpjCheckDigit(cnpj.AsSpan(0, 12)) == cnpj[12] - '0'
            && CnpjCheckDigit(cnpj.AsSpan(0, 13)) == cnpj[13] - '0';
    }

    /// <summary>Dígito verificador do CPF para os <paramref name="digits"/> anteriores (pesos decrescentes a partir de n + 1).</summary>
    public static int CpfCheckDigit(ReadOnlySpan<char> digits)
    {
        var sum = 0;
        for (var i = 0; i < digits.Length; i++)
        {
            sum += (digits[i] - '0') * (digits.Length + 1 - i);
        }

        var remainder = sum % 11;
        return remainder < 2 ? 0 : 11 - remainder;
    }

    /// <summary>Dígito verificador do CNPJ (pesos 2 a 9 da direita para a esquerda; letras valem ASCII − 48).</summary>
    public static int CnpjCheckDigit(ReadOnlySpan<char> characters)
    {
        var sum = 0;
        var weight = 2;
        for (var i = characters.Length - 1; i >= 0; i--)
        {
            sum += (characters[i] - '0') * weight;
            weight = weight == 9 ? 2 : weight + 1;
        }

        var remainder = sum % 11;
        return remainder < 2 ? 0 : 11 - remainder;
    }
}
