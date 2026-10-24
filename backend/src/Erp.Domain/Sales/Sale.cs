using Erp.Domain.Common;

namespace Erp.Domain.Sales;

public enum SaleStatus
{
    Draft = 1,
    Confirmed = 2,
    Cancelled = 3,
}

/// <summary>Forma de pagamento. Dinheiro, Pix e débito são à vista; crédito e boleto podem ser parcelados.</summary>
public enum PaymentMethod
{
    Cash = 1,
    Pix = 2,
    DebitCard = 3,
    CreditCard = 4,
    BankSlip = 5,
}

/// <summary>
/// Venda. Regras: só o rascunho é editado; confirmar baixa o estoque e gera as parcelas a receber (no
/// caso de uso, na mesma transação); cancelar a confirmada estorna os dois; nunca é excluída (o número
/// sequencial não pode ter lacuna). O desconto total (itens + venda) respeita o máximo da configuração.
/// </summary>
public sealed class Sale : Entity
{
    public const int NoteMaxLength = 500;
    public const int ReasonMaxLength = 300;
    public const int MaxInstallments = 12;

    private readonly List<SaleItem> _items = [];

    private Sale()
    {
    }

    /// <summary>Número sequencial sem lacunas, atribuído ao criar o rascunho.</summary>
    public int Number { get; private set; }

    public long PartnerId { get; private set; }

    public SaleStatus Status { get; private set; }

    public PaymentMethod PaymentMethod { get; private set; }

    public int Installments { get; private set; }

    /// <summary>Desconto da venda inteira (valor), além dos descontos dos itens.</summary>
    public decimal Discount { get; private set; }

    /// <summary>Soma dos itens já com os descontos deles.</summary>
    public decimal Subtotal { get; private set; }

    /// <summary>Subtotal menos o desconto da venda: o que o cliente paga.</summary>
    public decimal Total { get; private set; }

    /// <summary>Data da venda (calendário).</summary>
    public DateOnly IssuedAt { get; private set; }

    public DateTime? ConfirmedAt { get; private set; }

    public DateTime? CancelledAt { get; private set; }

    public string? CancellationReason { get; private set; }

    public string? Note { get; private set; }

    public IReadOnlyList<SaleItem> Items => _items;

    /// <summary>Soma de quantidade × preço, antes de qualquer desconto.</summary>
    public decimal Gross => _items.Sum(i => i.Quantity * i.UnitPrice);

    public static bool IsImmediate(PaymentMethod method) =>
        method is PaymentMethod.Cash or PaymentMethod.Pix or PaymentMethod.DebitCard;

    public static Sale CreateDraft(int number, SaleData data, IEnumerable<SaleItemData> items, decimal maxDiscountPercent)
    {
        var sale = new Sale { Number = number, Status = SaleStatus.Draft };
        sale.Apply(data, items, maxDiscountPercent);
        return sale;
    }

    public void UpdateDraft(SaleData data, IEnumerable<SaleItemData> items, decimal maxDiscountPercent)
    {
        EnsureStatus(SaleStatus.Draft, "editada");
        Apply(data, items, maxDiscountPercent);
    }

    /// <summary>
    /// Confirma o rascunho. O desconto é conferido de novo, porque o máximo da configuração pode ter
    /// mudado depois que o rascunho foi salvo.
    /// </summary>
    public void Confirm(DateTime now, decimal maxDiscountPercent)
    {
        EnsureStatus(SaleStatus.Draft, "confirmada");
        var errors = new ValidationErrors();
        CheckDiscount(errors, maxDiscountPercent);
        if (Total <= 0)
        {
            errors.Add("total", "Venda com total zero não é confirmada.");
        }
        else if (Total < Installments * 0.01m)
        {
            errors.Add("installments", "O total não cobre um centavo por parcela.");
        }

        errors.ThrowIfAny();
        Status = SaleStatus.Confirmed;
        ConfirmedAt = now;
    }

    /// <summary>Cancela rascunho ou venda confirmada (o caso de uso estorna estoque e parcelas da confirmada).</summary>
    public void Cancel(DateTime now, string? reason)
    {
        if (Status == SaleStatus.Cancelled)
        {
            throw new InvalidStateException("A venda já está cancelada.");
        }

        var trimmed = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
        new ValidationErrors().MaxLength("reason", trimmed, ReasonMaxLength).ThrowIfAny();
        Status = SaleStatus.Cancelled;
        CancelledAt = now;
        CancellationReason = trimmed;
    }

    private void Apply(SaleData data, IEnumerable<SaleItemData> items, decimal maxDiscountPercent)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(items);
        var note = string.IsNullOrWhiteSpace(data.Note) ? null : data.Note.Trim();
        var errors = new ValidationErrors()
            .PositiveId("partnerId", data.PartnerId)
            .NonNegativeDecimal("discount", data.Discount, DecimalLimits.MoneyScale, DecimalLimits.MoneyMax)
            .MaxLength("note", note, NoteMaxLength);

        if (!Enum.IsDefined(data.PaymentMethod))
        {
            errors.Add("paymentMethod", "Forma de pagamento inválida.");
        }
        else if (IsImmediate(data.PaymentMethod) && data.Installments != 1)
        {
            errors.Add("installments", "Pagamento à vista é em uma parcela.");
        }
        else if (data.Installments is < 1 or > MaxInstallments)
        {
            errors.Add("installments", $"Use de 1 a {MaxInstallments} parcelas.");
        }

        var newItems = items.Select((item, index) => SaleItem.Create(item, index, errors)).ToList();
        if (newItems.Count == 0)
        {
            errors.Add("items", "Inclua ao menos um item.");
        }

        errors.ThrowIfAny();

        // Itens do rascunho são substituídos (rascunho não tem histórico).
        _items.Clear();
        _items.AddRange(newItems);
        PartnerId = data.PartnerId;
        PaymentMethod = data.PaymentMethod;
        Installments = data.Installments;
        Discount = data.Discount;
        IssuedAt = data.IssuedAt;
        Note = note;
        Subtotal = _items.Sum(i => i.Total);
        Total = Subtotal - Discount;

        var totals = new ValidationErrors();
        if (Total < 0)
        {
            totals.Add("discount", "O desconto é maior que o valor dos itens.");
        }
        else
        {
            CheckDiscount(totals, maxDiscountPercent);
        }

        totals.ThrowIfAny();
    }

    /// <summary>Desconto total (itens + venda) sobre o bruto não passa do máximo da configuração.</summary>
    private void CheckDiscount(ValidationErrors errors, decimal maxDiscountPercent)
    {
        var gross = Gross;
        var discount = gross - Total;
        if (gross > 0 && discount > 0 && discount / gross * 100m > maxDiscountPercent)
        {
            errors.Add("discount", $"O desconto passa do máximo de {maxDiscountPercent:0.##}% (configuração da empresa).");
        }
    }

    private void EnsureStatus(SaleStatus expected, string action)
    {
        if (Status != expected)
        {
            throw new InvalidStateException(Status == SaleStatus.Cancelled
                ? $"Venda cancelada não pode ser {action}."
                : $"Venda confirmada não pode ser {action}; cancele-a se necessário.");
        }
    }
}

/// <summary>Dados editáveis da venda (sem os itens).</summary>
public sealed record SaleData(
    long PartnerId,
    PaymentMethod PaymentMethod,
    int Installments,
    decimal Discount,
    DateOnly IssuedAt,
    string? Note);
