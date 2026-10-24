using Erp.Domain.Common;

namespace Erp.Domain.Finance;

/// <summary>Conta a receber (cliente paga a empresa) ou a pagar (empresa paga o fornecedor).</summary>
public enum FinancialEntryKind
{
    Receivable = 1,
    Payable = 2,
}

/// <summary>Situação gravada. "Vencido" não é situação: é um lançamento aberto com vencimento passado.</summary>
public enum FinancialEntryStatus
{
    Open = 1,
    Paid = 2,
    Cancelled = 3,
}

/// <summary>
/// Lançamento financeiro: uma parcela de uma conta a receber ou a pagar. Regras: só o aberto é editado;
/// baixado (pago) não muda, só tem a baixa estornada (volta a aberto); aberto pode ser cancelado;
/// nunca é excluído. O atraso é calculado pelo vencimento.
/// </summary>
public sealed class FinancialEntry : Entity
{
    public const int DescriptionMaxLength = 200;
    public const int ReasonMaxLength = 300;
    public const int MaxInstallments = 60;

    private FinancialEntry()
    {
    }

    public FinancialEntryKind Kind { get; private set; }

    public long CategoryId { get; private set; }

    public long? PartnerId { get; private set; }

    public string Description { get; private set; } = string.Empty;

    public decimal Amount { get; private set; }

    public DateOnly DueDate { get; private set; }

    public FinancialEntryStatus Status { get; private set; }

    public DateOnly? PaidAt { get; private set; }

    public decimal? PaidAmount { get; private set; }

    /// <summary>Número da parcela (1 a <see cref="InstallmentTotal"/>).</summary>
    public int InstallmentNo { get; private set; }

    public int InstallmentTotal { get; private set; }

    /// <summary>Venda que gerou a parcela (Fase 6); nulo nos lançamentos manuais.</summary>
    public long? SaleId { get; private set; }

    /// <summary>Última vez que a baixa foi estornada (UTC) e o motivo; o histórico fica no log de auditoria futuro.</summary>
    public DateTime? ReversedAt { get; private set; }

    public string? ReversalReason { get; private set; }

    public DateTime? CancelledAt { get; private set; }

    public bool IsOverdue(DateOnly today) => Status == FinancialEntryStatus.Open && DueDate < today;

    /// <summary>
    /// Cria as parcelas de uma conta: <paramref name="totalAmount"/> dividido em
    /// <paramref name="installments"/> parcelas mensais a partir de <paramref name="firstDueDate"/>.
    /// Os centavos que sobram da divisão vão para a primeira parcela, para a soma fechar no total.
    /// </summary>
    public static IReadOnlyList<FinancialEntry> CreateInstallments(FinancialEntryData data, int installments)
    {
        ArgumentNullException.ThrowIfNull(data);
        var errors = Validate(data);
        if (installments is < 1 or > MaxInstallments)
        {
            errors.Add("installments", $"Use de 1 a {MaxInstallments} parcelas.");
        }
        else if (!errors.HasErrors && data.Amount < installments * 0.01m)
        {
            errors.Add("amount", "O valor não cobre um centavo por parcela.");
        }

        errors.ThrowIfAny();

        var share = Math.Floor(data.Amount / installments * 100m) / 100m;
        var first = data.Amount - (share * (installments - 1));
        var description = data.Description.Trim();
        return [.. Enumerable.Range(1, installments).Select(number => new FinancialEntry
        {
            Kind = data.Kind,
            CategoryId = data.CategoryId,
            PartnerId = data.PartnerId,
            Description = description,
            Amount = number == 1 ? first : share,
            DueDate = data.DueDate.AddMonths(number - 1),
            Status = FinancialEntryStatus.Open,
            InstallmentNo = number,
            InstallmentTotal = installments,
        })];
    }

    /// <summary>Edita um lançamento aberto (o tipo não muda).</summary>
    public void Update(FinancialEntryData data)
    {
        ArgumentNullException.ThrowIfNull(data);
        EnsureOpen("editado");
        var errors = Validate(data);
        if (data.Kind != Kind)
        {
            errors.Add("kind", "O tipo do lançamento não muda; cancele e lance de novo.");
        }

        errors.ThrowIfAny();
        CategoryId = data.CategoryId;
        PartnerId = data.PartnerId;
        Description = data.Description.Trim();
        Amount = data.Amount;
        DueDate = data.DueDate;
    }

    /// <summary>Baixa: registra a data e o valor pago (pode diferir do valor, por juros ou desconto).</summary>
    public void Pay(DateOnly paidAt, decimal paidAmount, DateOnly today)
    {
        EnsureOpen("baixado");
        var errors = new ValidationErrors()
            .NonNegativeDecimal("paidAmount", paidAmount, DecimalLimits.MoneyScale, DecimalLimits.MoneyMax);
        if (!errors.HasErrors && paidAmount == 0)
        {
            errors.Add("paidAmount", "Informe o valor pago.");
        }

        if (paidAt > today)
        {
            errors.Add("paidAt", "A data da baixa não pode ser no futuro.");
        }

        errors.ThrowIfAny();
        Status = FinancialEntryStatus.Paid;
        PaidAt = paidAt;
        PaidAmount = paidAmount;
    }

    /// <summary>Estorna a baixa: o lançamento volta a aberto, como se não tivesse sido pago.</summary>
    public void ReversePayment(string? reason, DateTime now)
    {
        if (Status != FinancialEntryStatus.Paid)
        {
            throw new InvalidStateException("Só um lançamento pago tem baixa para estornar.");
        }

        var trimmed = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
        new ValidationErrors().MaxLength("reason", trimmed, ReasonMaxLength).ThrowIfAny();
        Status = FinancialEntryStatus.Open;
        PaidAt = null;
        PaidAmount = null;
        ReversedAt = now;
        ReversalReason = trimmed;
    }

    /// <summary>Cancela um lançamento aberto. Pago precisa ter a baixa estornada antes.</summary>
    public void Cancel(DateTime now)
    {
        EnsureOpen("cancelado");
        Status = FinancialEntryStatus.Cancelled;
        CancelledAt = now;
    }

    private void EnsureOpen(string action)
    {
        if (Status != FinancialEntryStatus.Open)
        {
            throw new InvalidStateException(Status == FinancialEntryStatus.Paid
                ? $"Lançamento pago não pode ser {action}; estorne a baixa antes."
                : $"Lançamento cancelado não pode ser {action}.");
        }
    }

    private static ValidationErrors Validate(FinancialEntryData data)
    {
        var errors = new ValidationErrors()
            .Required("description", data.Description, DescriptionMaxLength)
            .PositiveId("categoryId", data.CategoryId)
            .OptionalPositiveId("partnerId", data.PartnerId)
            .NonNegativeDecimal("amount", data.Amount, DecimalLimits.MoneyScale, DecimalLimits.MoneyMax);
        if (!Enum.IsDefined(data.Kind))
        {
            errors.Add("kind", "Tipo inválido.");
        }

        if (!errors.HasErrors && data.Amount == 0)
        {
            errors.Add("amount", "Informe um valor maior que zero.");
        }

        return errors;
    }
}

/// <summary>Dados editáveis de um lançamento.</summary>
public sealed record FinancialEntryData(
    FinancialEntryKind Kind,
    string Description,
    long CategoryId,
    long? PartnerId,
    decimal Amount,
    DateOnly DueDate);
