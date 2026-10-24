namespace Erp.Application.Common;

/// <summary>Recurso inexistente. Vira 404 com o código <see cref="Code"/>.</summary>
public sealed class NotFoundException : Exception
{
    public NotFoundException()
        : this("resource.not_found", "Registro não encontrado.")
    {
    }

    public NotFoundException(string message)
        : this("resource.not_found", message)
    {
    }

    public NotFoundException(string message, Exception innerException)
        : base(message, innerException)
    {
        Code = "resource.not_found";
    }

    public NotFoundException(string code, string message)
        : base(message)
    {
        Code = code;
    }

    public string Code { get; }
}

/// <summary>Conflito com o estado atual (duplicidade, versão desatualizada, registro em uso). Vira 409.</summary>
public sealed class ConflictException : Exception
{
    public ConflictException()
        : this("conflict", "Conflito com o estado atual do registro.")
    {
    }

    public ConflictException(string message)
        : this("conflict", message)
    {
    }

    public ConflictException(string message, Exception innerException)
        : base(message, innerException)
    {
        Code = "conflict";
    }

    public ConflictException(string code, string message)
        : base(message)
    {
        Code = code;
    }

    public string Code { get; }

    public static ConflictException StaleVersion() =>
        new(ErrorCodes.ConcurrencyConflict, "O registro foi alterado por outra pessoa. Recarregue e tente de novo.");
}

/// <summary>Credenciais inválidas, conta bloqueada ou sessão expirada. Vira 401.</summary>
public sealed class UnauthorizedException : Exception
{
    public UnauthorizedException()
        : this(ErrorCodes.InvalidCredentials, "E-mail ou senha inválidos.")
    {
    }

    public UnauthorizedException(string message)
        : this(ErrorCodes.InvalidCredentials, message)
    {
    }

    public UnauthorizedException(string message, Exception innerException)
        : base(message, innerException)
    {
        Code = ErrorCodes.InvalidCredentials;
    }

    public UnauthorizedException(string code, string message)
        : base(message)
    {
        Code = code;
    }

    public string Code { get; }
}

/// <summary>Códigos de erro estáveis, devolvidos no campo <c>code</c> do ProblemDetails.</summary>
public static class ErrorCodes
{
    public const string ValidationFailed = "validation_failed";
    public const string ConcurrencyConflict = "concurrency_conflict";
    public const string DuplicateKey = "duplicate_key";
    public const string NotFound = "not_found";
    public const string Unexpected = "unexpected_error";

    public const string ProductNotFound = "product.not_found";
    public const string ProductSkuDuplicate = "product.sku_duplicate";
    public const string ProductCategoryNotFound = "product_category.not_found";
    public const string ProductCategoryNameDuplicate = "product_category.name_duplicate";
    public const string ProductCategoryInUse = "product_category.in_use";
    public const string UnitNotFound = "unit.not_found";
    public const string UnitCodeDuplicate = "unit.code_duplicate";
    public const string UnitInUse = "unit.in_use";

    public const string PartnerNotFound = "partner.not_found";
    public const string PartnerDocumentDuplicate = "partner.document_duplicate";

    public const string StockMovementNotFound = "stock_movement.not_found";
    public const string StockMovementAlreadyReversed = "stock_movement.already_reversed";

    public const string InvalidState = "invalid_state";
    public const string FinancialCategoryNotFound = "financial_category.not_found";
    public const string FinancialCategoryNameDuplicate = "financial_category.name_duplicate";
    public const string FinancialCategoryInUse = "financial_category.in_use";
    public const string FinancialEntryNotFound = "financial_entry.not_found";

    public const string InvalidCredentials = "auth.invalid_credentials";
    public const string LockedOut = "auth.locked_out";
    public const string InvalidRefreshToken = "auth.invalid_refresh_token";
    public const string Forbidden = "auth.forbidden";
    public const string Unauthenticated = "auth.unauthenticated";
    public const string UserNotFound = "user.not_found";
    public const string UserEmailDuplicate = "user.email_duplicate";
    public const string UserSelfLockout = "user.self_lockout";
}
