using Erp.Application.Dashboard;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Erp.Api.Dashboard;

/// <summary>Números da tela inicial.</summary>
internal static class DashboardEndpoints
{
    public static RouteGroupBuilder MapDashboardEndpoints(this RouteGroupBuilder api)
    {
        api.MapGet("/dashboard", GetAsync).WithTags("Dashboard").WithName("GetDashboard");
        return api;
    }

    private static async Task<Ok<DashboardDto>> GetAsync(DashboardService service, CancellationToken cancellationToken) =>
        TypedResults.Ok(await service.GetAsync(cancellationToken));
}
