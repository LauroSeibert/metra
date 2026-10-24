using System.Text.Json.Serialization;
using Erp.Api.Access;
using Erp.Api.ErrorHandling;
using Erp.Api.OpenApi;
using Erp.Api.Partners;
using Erp.Api.Products;
using Erp.Application;
using Erp.Infrastructure;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

const string FrontendCorsPolicy = "frontend";
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

// Números só como número no JSON (o padrão web aceita string, o que sujaria o contrato OpenAPI).
// Enums como texto ("Customer"), legíveis no contrato e estáveis se a ordem mudar.
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.NumberHandling = JsonNumberHandling.Strict;
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddOpenApi("v1", options =>
{
    options.OpenApiVersion = OpenApiSpecVersion.OpenApi3_0;
    options.AddSchemaTransformer<RequiredPropertiesSchemaTransformer>();
});
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddCors(options => options.AddPolicy(FrontendCorsPolicy, policy => policy
    .WithOrigins(allowedOrigins)
    .AllowAnyHeader()
    .AllowAnyMethod()
    .AllowCredentials()));
builder.Services.AddErpAuthorization();
builder.Services.AddLoginRateLimit(builder.Configuration);
// Atrás do nginx, o IP do cliente vem em X-Forwarded-For (limite de tentativas de login por IP).
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

app.UseForwardedHeaders();
app.UseExceptionHandler();
app.UseStatusCodePages();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseCors(FrontendCorsPolicy);
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi().AllowAnonymous();
}

// Liveness: o processo responde. Readiness: dependências (banco) disponíveis.
app.MapHealthChecks("/health", new HealthCheckOptions { Predicate = _ => false }).AllowAnonymous();
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains(Erp.Infrastructure.DependencyInjection.ReadyTag),
}).AllowAnonymous();

app.MapGroup("/api/v1")
    .MapAuthEndpoints()
    .MapProfileEndpoints()
    .MapUserEndpoints()
    .MapProductEndpoints()
    .MapProductCategoryEndpoints()
    .MapUnitEndpoints()
    .MapPartnerEndpoints();

await app.RunAsync().ConfigureAwait(false);

// Exposto para WebApplicationFactory nos testes de integração.
public partial class Program;
