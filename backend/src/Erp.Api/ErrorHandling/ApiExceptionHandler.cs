using Erp.Application.Common;
using Erp.Domain.Common;
using Erp.Infrastructure.Persistence;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Erp.Api.ErrorHandling;

/// <summary>
/// Converte exceções de negócio em ProblemDetails (RFC 9457) com o campo <c>code</c> estável:
/// 400 validação, 404 não encontrado, 409 conflito. O resto vira 500 sem detalhes internos.
/// </summary>
internal sealed partial class ApiExceptionHandler(IProblemDetailsService problemDetails, ILogger<ApiExceptionHandler> logger)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        ProblemDetails problem = exception switch
        {
            DomainValidationException validation => new HttpValidationProblemDetails(
                validation.Errors.ToDictionary(pair => pair.Key, pair => pair.Value))
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Dados inválidos.",
                Detail = validation.Message,
                Extensions = { ["code"] = ErrorCodes.ValidationFailed },
            },
            NotFoundException notFound => Problem(StatusCodes.Status404NotFound, "Não encontrado.", notFound.Message, notFound.Code),
            UnauthorizedException unauthorized => Problem(StatusCodes.Status401Unauthorized, "Não autorizado.", unauthorized.Message, unauthorized.Code),
            ConflictException conflict => Problem(StatusCodes.Status409Conflict, "Conflito.", conflict.Message, conflict.Code),
            DbUpdateConcurrencyException => Problem(
                StatusCodes.Status409Conflict,
                "Conflito.",
                ConflictException.StaleVersion().Message,
                ErrorCodes.ConcurrencyConflict),
            DbUpdateException when DbExceptionClassifier.IsUniqueViolation(exception) => Problem(
                StatusCodes.Status409Conflict,
                "Conflito.",
                "Já existe um registro com esses dados.",
                ErrorCodes.DuplicateKey),
            BadHttpRequestException badRequest => Problem(
                StatusCodes.Status400BadRequest,
                "Requisição inválida.",
                badRequest.Message,
                ErrorCodes.ValidationFailed),
            _ => Problem(StatusCodes.Status500InternalServerError, "Erro inesperado.", null, ErrorCodes.Unexpected),
        };

        if (problem.Status == StatusCodes.Status500InternalServerError)
        {
            LogUnexpected(logger, exception);
        }

        httpContext.Response.StatusCode = problem.Status ?? StatusCodes.Status500InternalServerError;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problem,
            Exception = exception,
        }).ConfigureAwait(false);
    }

    private static ProblemDetails Problem(int status, string title, string? detail, string code) => new()
    {
        Status = status,
        Title = title,
        Detail = detail,
        Extensions = { ["code"] = code },
    };

    [LoggerMessage(Level = LogLevel.Error, Message = "Erro não tratado na requisição")]
    private static partial void LogUnexpected(ILogger logger, Exception exception);
}
