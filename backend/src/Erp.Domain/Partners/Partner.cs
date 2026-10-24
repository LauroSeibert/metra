using System.Text.RegularExpressions;
using Erp.Domain.Common;

namespace Erp.Domain.Partners;

/// <summary>Papel do parceiro nas operações: cliente (vendas), fornecedor (compras e contas a pagar) ou ambos.</summary>
public enum PartnerType
{
    Customer = 1,
    Supplier = 2,
    Both = 3,
}

/// <summary>
/// Cliente ou fornecedor. Regras: documento (CPF ou CNPJ) obrigatório, válido e único (gravado
/// normalizado, sem pontuação); nunca excluído: parceiro fora de uso é inativado.
/// </summary>
public sealed partial class Partner : Entity
{
    public const int NameMaxLength = 200;
    public const int DocumentMaxLength = 14;
    public const int EmailMaxLength = 200;
    public const int PhoneMaxLength = 20;

    private Partner()
    {
    }

    public PartnerType Type { get; private set; }

    public string Name { get; private set; } = string.Empty;

    /// <summary>CPF ou CNPJ normalizado (só letras maiúsculas e dígitos).</summary>
    public string Document { get; private set; } = string.Empty;

    public TaxDocumentKind DocumentKind { get; private set; }

    public string? Email { get; private set; }

    public string? Phone { get; private set; }

    public bool Active { get; private set; }

    public bool IsCustomer => Type is PartnerType.Customer or PartnerType.Both;

    public bool IsSupplier => Type is PartnerType.Supplier or PartnerType.Both;

    public static Partner Create(PartnerData data)
    {
        var partner = new Partner { Active = true };
        partner.Apply(data);
        return partner;
    }

    public void Update(PartnerData data) => Apply(data);

    public void Activate() => Active = true;

    public void Deactivate() => Active = false;

    private void Apply(PartnerData data)
    {
        ArgumentNullException.ThrowIfNull(data);

        var document = data.Document is null ? null : TaxDocument.Normalize(data.Document);
        var email = string.IsNullOrWhiteSpace(data.Email) ? null : data.Email.Trim().ToLowerInvariant();
        var phone = string.IsNullOrWhiteSpace(data.Phone) ? null : data.Phone.Trim();

        var errors = new ValidationErrors()
            .Required("name", data.Name, NameMaxLength)
            .MaxLength("email", email, EmailMaxLength)
            .MaxLength("phone", phone, PhoneMaxLength);

        if (!Enum.IsDefined(data.Type))
        {
            errors.Add("type", "Tipo inválido.");
        }

        if (string.IsNullOrEmpty(document))
        {
            errors.Add("document", "Campo obrigatório.");
        }
        else if (!TaxDocument.IsValid(document))
        {
            errors.Add("document", "CPF ou CNPJ inválido.");
        }

        if (email is not null && email.Length <= EmailMaxLength && !EmailPattern().IsMatch(email))
        {
            errors.Add("email", "E-mail inválido.");
        }

        errors.ThrowIfAny();

        Type = data.Type;
        Name = data.Name.Trim();
        Document = document!;
        DocumentKind = TaxDocument.KindOf(document!)!.Value;
        Email = email;
        Phone = phone;
    }

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.CultureInvariant)]
    private static partial Regex EmailPattern();
}

/// <summary>Dados editáveis de um parceiro. O documento pode vir formatado; é gravado normalizado.</summary>
public sealed record PartnerData(PartnerType Type, string Name, string? Document, string? Email, string? Phone);
