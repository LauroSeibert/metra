using System.Text.Json.Serialization;
using Erp.Api.ErrorHandling;
using Erp.Api.OpenApi;
using Erp.Api.Products;
using Erp.Application;
using Erp.Infrastructure;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

const string FrontendCorsPolicy = "frontend";
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

// Números só como número no JSON (o padrão web aceita string, o que sujaria o contrato OpenAPI).
builder.Services.ConfigureHttpJsonOptions(options => options.SerializerOptions.NumberHandling = JsonNumberHandling.Strict);
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
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseCors(FrontendCorsPolicy);

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Liveness: o processo responde. Readiness: dependências (banco) disponíveis.
app.MapHealthChecks("/health", new HealthCheckOptions { Predicate = _ => false });
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains(Erp.Infrastructure.DependencyInjection.ReadyTag),
});

app.MapGroup("/api/v1")
    .MapProductEndpoints()
    .MapProductCategoryEndpoints()
    .MapUnitEndpoints();

await app.RunAsync().ConfigureAwait(false);

// Exposto para WebApplicationFactory nos testes de integração.
public partial class Program;
