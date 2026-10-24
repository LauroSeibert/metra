using System.Net;
using System.Net.Http.Json;
using Erp.Application.Common;
using Erp.Application.Partners;
using Erp.Domain.Access;
using Erp.Domain.Partners;
using Erp.IntegrationTests.Infrastructure;
using static Erp.IntegrationTests.Infrastructure.ApiClient;

namespace Erp.IntegrationTests.Partners;

public sealed class PartnerEndpointsTests(MySqlApiFixture fixture)
{
    private readonly HttpClient _client = fixture.ClientFor(Roles.Operator);

    [Fact]
    public async Task Create_NormalizesFormattedDocument_AndDetectsKind()
    {
        var cnpj = NewCnpj();
        var formatted = $"{cnpj[..2]}.{cnpj[2..5]}.{cnpj[5..8]}/{cnpj[8..12]}-{cnpj[12..]}";

        var partner = await _client.CreatePartnerAsync(PartnerType.Supplier, formatted);

        Assert.Equal(cnpj, partner.Document);
        Assert.Equal(TaxDocumentKind.Cnpj, partner.DocumentKind);
        Assert.Equal(1, partner.Version);
        Assert.True(partner.Active);
    }

    [Fact]
    public async Task Create_AcceptsAlphanumericCnpj()
    {
        var cnpj = NewCnpj(alphanumeric: true);

        var partner = await _client.CreatePartnerAsync(PartnerType.Supplier, cnpj.ToLowerInvariant());

        Assert.Equal(cnpj, partner.Document);
    }

    [Fact]
    public async Task Create_InvalidDocumentAndEmail_Returns400WithFields()
    {
        using var response = await _client.PostAsJsonAsync(
            new Uri("/api/v1/partners", UriKind.Relative),
            new CreatePartnerRequest(PartnerType.Customer, "", "111.111.111-11", "sem-arroba", null),
            Json,
            Ct);

        var problem = await response.ReadProblemAsync(HttpStatusCode.BadRequest);
        Assert.Equal(["document", "email", "name"], problem.ErrorFields());
    }

    [Fact]
    public async Task Create_DuplicateDocument_EvenFormattedDifferently_Returns409()
    {
        var cpf = NewCpf();
        await _client.CreatePartnerAsync(document: cpf);

        using var response = await _client.PostAsJsonAsync(
            new Uri("/api/v1/partners", UriKind.Relative),
            new CreatePartnerRequest(PartnerType.Supplier, "Outro", $"{cpf[..3]}.{cpf[3..6]}.{cpf[6..9]}-{cpf[9..]}", null, null),
            Json,
            Ct);

        var problem = await response.ReadProblemAsync(HttpStatusCode.Conflict);
        Assert.Equal(ErrorCodes.PartnerDocumentDuplicate, problem.Code());
    }

    [Fact]
    public async Task Update_ChangesData_AndRejectsStaleVersion()
    {
        var partner = await _client.CreatePartnerAsync();

        using var update = await _client.PutAsJsonAsync(
            new Uri($"/api/v1/partners/{partner.Id}", UriKind.Relative),
            new UpdatePartnerRequest(PartnerType.Both, "Nome novo", partner.Document, "Contato@Empresa.com.br", "(11) 99999-0000", partner.Version),
            Json,
            Ct);
        update.EnsureSuccessStatusCode();
        var updated = (await update.Content.ReadFromJsonAsync<PartnerDto>(Json, Ct))!;
        Assert.Equal((PartnerType.Both, "contato@empresa.com.br", 2), (updated.Type, updated.Email, updated.Version));

        using var stale = await _client.PutAsJsonAsync(
            new Uri($"/api/v1/partners/{partner.Id}", UriKind.Relative),
            new UpdatePartnerRequest(PartnerType.Customer, "De novo", partner.Document, null, null, partner.Version),
            Json,
            Ct);
        var problem = await stale.ReadProblemAsync(HttpStatusCode.Conflict);
        Assert.Equal(ErrorCodes.ConcurrencyConflict, problem.Code());
    }

    [Fact]
    public async Task List_TypeFilter_IncludesPartnersThatAreBoth()
    {
        var prefix = Unique("Tipo ");
        await _client.CreatePartnerAsync(PartnerType.Customer, name: $"{prefix} cliente");
        await _client.CreatePartnerAsync(PartnerType.Supplier, name: $"{prefix} fornecedor");
        await _client.CreatePartnerAsync(PartnerType.Both, name: $"{prefix} ambos");

        var customers = await ListAsync($"search={Uri.EscapeDataString(prefix)}&type=Customer");
        var suppliers = await ListAsync($"search={Uri.EscapeDataString(prefix)}&type=Supplier");
        var both = await ListAsync($"search={Uri.EscapeDataString(prefix)}&type=Both");

        Assert.Equal([$"{prefix} ambos", $"{prefix} cliente"], customers.Items.Select(p => p.Name));
        Assert.Equal([$"{prefix} ambos", $"{prefix} fornecedor"], suppliers.Items.Select(p => p.Name));
        Assert.Equal([$"{prefix} ambos"], both.Items.Select(p => p.Name));
    }

    [Fact]
    public async Task List_SearchesDocumentPrefix_TypedWithPunctuation()
    {
        var cpf = NewCpf();
        var partner = await _client.CreatePartnerAsync(document: cpf);

        var found = await ListAsync($"search={Uri.EscapeDataString($"{cpf[..3]}.{cpf[3..6]}.{cpf[6..9]}")}");

        Assert.Contains(found.Items, p => p.Id == partner.Id);
    }

    [Fact]
    public async Task Deactivate_ThenActivate_AndPartnerIsNeverDeleted()
    {
        var partner = await _client.CreatePartnerAsync();

        using var deactivate = await _client.PostAsync(new Uri($"/api/v1/partners/{partner.Id}/deactivate", UriKind.Relative), null, Ct);
        var inactive = (await deactivate.Content.ReadFromJsonAsync<PartnerDto>(Json, Ct))!;
        using var activate = await _client.PostAsync(new Uri($"/api/v1/partners/{partner.Id}/activate", UriKind.Relative), null, Ct);
        var active = (await activate.Content.ReadFromJsonAsync<PartnerDto>(Json, Ct))!;
        using var delete = await _client.DeleteAsync(new Uri($"/api/v1/partners/{partner.Id}", UriKind.Relative), Ct);

        Assert.False(inactive.Active);
        Assert.True(active.Active);
        Assert.Equal(HttpStatusCode.MethodNotAllowed, delete.StatusCode);
    }

    [Fact]
    public async Task Get_Unknown_Returns404()
    {
        using var response = await _client.GetAsync(new Uri("/api/v1/partners/999999999", UriKind.Relative), Ct);

        var problem = await response.ReadProblemAsync(HttpStatusCode.NotFound);
        Assert.Equal(ErrorCodes.PartnerNotFound, problem.Code());
    }

    private async Task<PagedResult<PartnerListItemDto>> ListAsync(string query)
    {
        using var response = await _client.GetAsync(new Uri($"/api/v1/partners?{query}", UriKind.Relative), Ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<PagedResult<PartnerListItemDto>>(Json, Ct))!;
    }
}
