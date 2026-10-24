using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Erp.Application.Access;
using Erp.Application.Common;
using Erp.Domain.Access;
using Erp.IntegrationTests.Infrastructure;
using static Erp.IntegrationTests.Infrastructure.ApiClient;

namespace Erp.IntegrationTests.Access;

/// <summary>Login, bloqueio por tentativas, refresh, saída, perfil e administração de usuários.</summary>
public sealed class SessionTests(MySqlApiFixture fixture)
{
    private HttpClient Admin => fixture.ClientFor(Roles.Admin);

    [Fact]
    public async Task Login_WithWrongPassword_Returns401()
    {
        var email = await CreateUserAsync(Roles.Viewer);

        using var response = await PostLoginAsync(email, "Errada#123");

        var problem = await response.ReadProblemAsync(HttpStatusCode.Unauthorized);
        Assert.Equal(ErrorCodes.InvalidCredentials, problem.Code());
    }

    [Fact]
    public async Task Login_UnknownEmail_LooksLikeAWrongPassword()
    {
        using var response = await PostLoginAsync("ninguem@test.local", MySqlApiFixture.Password);

        var problem = await response.ReadProblemAsync(HttpStatusCode.Unauthorized);
        Assert.Equal(ErrorCodes.InvalidCredentials, problem.Code());
    }

    [Fact]
    public async Task FiveWrongPasswords_LockTheAccount_EvenForTheRightPassword()
    {
        var email = await CreateUserAsync(Roles.Viewer);
        for (var attempt = 1; attempt <= 5; attempt++)
        {
            using var wrong = await PostLoginAsync(email, "Errada#123");
            Assert.Equal(HttpStatusCode.Unauthorized, wrong.StatusCode);
        }

        using var right = await PostLoginAsync(email, MySqlApiFixture.Password);

        var problem = await right.ReadProblemAsync(HttpStatusCode.Unauthorized);
        Assert.Equal(ErrorCodes.LockedOut, problem.Code());
    }

    [Fact]
    public async Task AdminPasswordReset_UnlocksTheAccount()
    {
        var email = await CreateUserAsync(Roles.Viewer);
        for (var attempt = 1; attempt <= 5; attempt++)
        {
            using var wrong = await PostLoginAsync(email, "Errada#123");
        }

        var user = await FindUserAsync(email);
        Assert.True(user.LockedOut);
        using (var reset = await Admin.PostAsJsonAsync(
            new Uri($"/api/v1/users/{user.Id}/reset-password", UriKind.Relative), new ResetPasswordRequest("Nova#12345"), Ct))
        {
            Assert.Equal(HttpStatusCode.NoContent, reset.StatusCode);
        }

        using var login = await PostLoginAsync(email, "Nova#12345");
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
    }

    [Fact]
    public async Task Refresh_IssuesNewTokens_AndLogoutRevokesIt()
    {
        var email = await CreateUserAsync(Roles.Operator);
        var tokens = await LoginTokensAsync(email);

        using (var refresh = await PostRefreshAsync(tokens.RefreshToken))
        {
            refresh.EnsureSuccessStatusCode();
            var renewed = (await refresh.Content.ReadFromJsonAsync<SessionTokensDto>(Ct))!;
            Assert.False(string.IsNullOrEmpty(renewed.AccessToken));
            Assert.Equal(900, renewed.ExpiresIn);
        }

        using var client = fixture.Anonymous();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);
        using (var logout = await client.PostAsync(new Uri("/api/v1/auth/logout", UriKind.Relative), null, Ct))
        {
            Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);
        }

        using var afterLogout = await PostRefreshAsync(tokens.RefreshToken);
        var problem = await afterLogout.ReadProblemAsync(HttpStatusCode.Unauthorized);
        Assert.Equal(ErrorCodes.InvalidRefreshToken, problem.Code());
    }

    [Fact]
    public async Task Refresh_WithGarbage_Returns401()
    {
        using var response = await PostRefreshAsync("nao-e-um-token");

        var problem = await response.ReadProblemAsync(HttpStatusCode.Unauthorized);
        Assert.Equal(ErrorCodes.InvalidRefreshToken, problem.Code());
    }

    [Fact]
    public async Task DeactivatedUser_CannotLoginNorRefresh()
    {
        var email = await CreateUserAsync(Roles.Operator);
        var tokens = await LoginTokensAsync(email);
        var user = await FindUserAsync(email);

        using (var update = await Admin.PutAsJsonAsync(
            new Uri($"/api/v1/users/{user.Id}", UriKind.Relative),
            new UpdateUserRequest(user.FullName, user.Role, false, user.Version),
            Ct))
        {
            update.EnsureSuccessStatusCode();
        }

        using var login = await PostLoginAsync(email, MySqlApiFixture.Password);
        using var refresh = await PostRefreshAsync(tokens.RefreshToken);
        Assert.Equal(HttpStatusCode.Unauthorized, login.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, refresh.StatusCode);
    }

    [Fact]
    public async Task Admin_CannotLockThemselvesOut()
    {
        var me = (await Admin.GetFromJsonAsync<UserProfileDto>(new Uri("/api/v1/me", UriKind.Relative), Json, Ct))!;

        using var response = await Admin.PutAsJsonAsync(
            new Uri($"/api/v1/users/{me.Id}", UriKind.Relative),
            new UpdateUserRequest(me.FullName, Roles.Viewer, true, me.Version),
            Ct);

        var problem = await response.ReadProblemAsync(HttpStatusCode.Conflict);
        Assert.Equal(ErrorCodes.UserSelfLockout, problem.Code());
    }

    [Fact]
    public async Task CreateUser_WithWeakPassword_Returns400OnPassword()
    {
        using var response = await Admin.PostAsJsonAsync(
            new Uri("/api/v1/users", UriKind.Relative),
            new CreateUserRequest($"{Unique("fraca")}@test.local", "Senha fraca", Roles.Viewer, "123"),
            Ct);

        var problem = await response.ReadProblemAsync(HttpStatusCode.BadRequest);
        Assert.Equal(["password"], problem.ErrorFields());
    }

    [Fact]
    public async Task CreateUser_DuplicateEmail_Returns409()
    {
        var email = await CreateUserAsync(Roles.Viewer);

        using var response = await Admin.PostAsJsonAsync(
            new Uri("/api/v1/users", UriKind.Relative),
            new CreateUserRequest(email.ToUpperInvariant(), "Repetido", Roles.Viewer, MySqlApiFixture.Password),
            Ct);

        var problem = await response.ReadProblemAsync(HttpStatusCode.Conflict);
        Assert.Equal(ErrorCodes.UserEmailDuplicate, problem.Code());
    }

    [Fact]
    public async Task Profile_UpdatesNameThemeAndPageSize_WithVersion()
    {
        var email = await CreateUserAsync(Roles.Viewer);
        using var client = await fixture.LoginAsync(email, MySqlApiFixture.Password);
        var profile = (await client.GetFromJsonAsync<UserProfileDto>(new Uri("/api/v1/me", UriKind.Relative), Json, Ct))!;

        using var update = await client.PutAsJsonAsync(
            new Uri("/api/v1/me", UriKind.Relative),
            new UpdateProfileRequest("Nome Novo", ThemePreference.Dark, 50, profile.Version),
            Ct);
        update.EnsureSuccessStatusCode();
        var updated = (await update.Content.ReadFromJsonAsync<UserProfileDto>(Json, Ct))!;

        Assert.Equal(("Nome Novo", ThemePreference.Dark, 50), (updated.FullName, updated.Theme, updated.PageSize));
        Assert.Equal(profile.Version + 1, updated.Version);

        using var stale = await client.PutAsJsonAsync(
            new Uri("/api/v1/me", UriKind.Relative),
            new UpdateProfileRequest("Outro", ThemePreference.Light, 20, profile.Version),
            Ct);
        Assert.Equal(HttpStatusCode.Conflict, stale.StatusCode);
    }

    [Fact]
    public async Task Profile_RejectsUnsupportedPageSize()
    {
        var email = await CreateUserAsync(Roles.Viewer);
        using var client = await fixture.LoginAsync(email, MySqlApiFixture.Password);
        var profile = (await client.GetFromJsonAsync<UserProfileDto>(new Uri("/api/v1/me", UriKind.Relative), Json, Ct))!;

        using var response = await client.PutAsJsonAsync(
            new Uri("/api/v1/me", UriKind.Relative),
            new UpdateProfileRequest("Nome", ThemePreference.Light, 33, profile.Version),
            Ct);

        var problem = await response.ReadProblemAsync(HttpStatusCode.BadRequest);
        Assert.Equal(["pageSize"], problem.ErrorFields());
    }

    [Fact]
    public async Task ChangePassword_RequiresTheCurrentOne_AndTheNewOneWorks()
    {
        var email = await CreateUserAsync(Roles.Viewer);
        using var client = await fixture.LoginAsync(email, MySqlApiFixture.Password);

        using (var wrong = await client.PostAsJsonAsync(
            new Uri("/api/v1/me/password", UriKind.Relative), new ChangePasswordRequest("Errada#123", "Outra#12345"), Ct))
        {
            var problem = await wrong.ReadProblemAsync(HttpStatusCode.BadRequest);
            Assert.Equal(["currentPassword"], problem.ErrorFields());
        }

        using (var right = await client.PostAsJsonAsync(
            new Uri("/api/v1/me/password", UriKind.Relative), new ChangePasswordRequest(MySqlApiFixture.Password, "Outra#12345"), Ct))
        {
            Assert.Equal(HttpStatusCode.NoContent, right.StatusCode);
        }

        using var login = await PostLoginAsync(email, "Outra#12345");
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
    }

    private async Task<string> CreateUserAsync(string role)
    {
        var email = $"{Unique("u").ToLowerInvariant()}@test.local";
        using var response = await Admin.PostAsJsonAsync(
            new Uri("/api/v1/users", UriKind.Relative),
            new CreateUserRequest(email, "Usuário de teste", role, MySqlApiFixture.Password),
            Ct);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return email;
    }

    private async Task<UserDto> FindUserAsync(string email)
    {
        var page = await Admin.GetFromJsonAsync<PagedResult<UserDto>>(
            new Uri($"/api/v1/users?search={Uri.EscapeDataString(email)}", UriKind.Relative), Ct);
        return Assert.Single(page!.Items);
    }

    private async Task<HttpResponseMessage> PostLoginAsync(string email, string password)
    {
        using var client = fixture.Anonymous();
        return await client.PostAsJsonAsync(new Uri("/api/v1/auth/login", UriKind.Relative), new LoginRequest(email, password), Ct);
    }

    private async Task<HttpResponseMessage> PostRefreshAsync(string refreshToken)
    {
        using var client = fixture.Anonymous();
        return await client.PostAsJsonAsync(new Uri("/api/v1/auth/refresh", UriKind.Relative), new RefreshRequest(refreshToken), Ct);
    }

    private async Task<SessionTokensDto> LoginTokensAsync(string email)
    {
        using var response = await PostLoginAsync(email, MySqlApiFixture.Password);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<SessionTokensDto>(Ct))!;
    }
}
