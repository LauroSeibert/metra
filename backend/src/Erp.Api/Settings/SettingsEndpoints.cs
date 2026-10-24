using Erp.Api.Access;
using Erp.Application.Settings;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Erp.Api.Settings;

/// <summary>Parâmetros da empresa: qualquer perfil lê, só Admin altera.</summary>
internal static class SettingsEndpoints
{
    public static RouteGroupBuilder MapSettingsEndpoints(this RouteGroupBuilder api)
    {
        var group = api.MapGroup("/settings").WithTags("Settings");
        group.MapGet("/", GetAsync).WithName("GetCompanySettings");
        group.MapPut("/", UpdateAsync)
            .WithName("UpdateCompanySettings")
            .RequireAuthorization(Policies.Admin)
            .ProducesValidationProblem();
        return api;
    }

    private static async Task<Ok<CompanySettingsDto>> GetAsync(SettingsService service, CancellationToken cancellationToken) =>
        TypedResults.Ok(await service.GetAsync(cancellationToken));

    private static async Task<Ok<CompanySettingsDto>> UpdateAsync(
        CompanySettingsDto request,
        SettingsService service,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await service.UpdateAsync(request, cancellationToken));
}
