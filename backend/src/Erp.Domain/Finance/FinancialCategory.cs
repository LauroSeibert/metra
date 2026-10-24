using Erp.Domain.Common;

namespace Erp.Domain.Finance;

/// <summary>Natureza da categoria: receita (contas a receber) ou despesa (contas a pagar).</summary>
public enum FinancialCategoryKind
{
    Revenue = 1,
    Expense = 2,
}

/// <summary>
/// Categoria financeira (vendas, aluguel, fornecedores...). Nome único dentro do tipo; não é excluída
/// enquanto houver lançamento nela.
/// </summary>
public sealed class FinancialCategory : Entity
{
    public const int NameMaxLength = 100;

    private FinancialCategory()
    {
    }

    public string Name { get; private set; } = string.Empty;

    public FinancialCategoryKind Kind { get; private set; }

    public static FinancialCategory Create(string name, FinancialCategoryKind kind)
    {
        var category = new FinancialCategory();
        category.Update(name, kind);
        return category;
    }

    public void Update(string name, FinancialCategoryKind kind)
    {
        var errors = new ValidationErrors().Required("name", name, NameMaxLength);
        if (!Enum.IsDefined(kind))
        {
            errors.Add("kind", "Tipo inválido.");
        }

        errors.ThrowIfAny();
        Name = name.Trim();
        Kind = kind;
    }

    /// <summary>Categoria que serve para o tipo de lançamento (a receber usa receita; a pagar, despesa).</summary>
    public bool Fits(FinancialEntryKind entryKind) =>
        (Kind, entryKind) is (FinancialCategoryKind.Revenue, FinancialEntryKind.Receivable)
            or (FinancialCategoryKind.Expense, FinancialEntryKind.Payable);
}
