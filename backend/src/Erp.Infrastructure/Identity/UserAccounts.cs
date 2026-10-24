using Erp.Application.Abstractions;
using Erp.Application.Access;
using Erp.Application.Common;
using Erp.Domain.Access;
using Erp.Domain.Common;
using Erp.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Erp.Infrastructure.Identity;

/// <summary>Contas de usuário sobre o ASP.NET Core Identity (senha, bloqueio, perfis).</summary>
internal sealed class UserAccounts(
    UserManager<AppUser> users,
    ErpDbContext db,
    TimeProvider clock) : IUserAccounts
{
    public static readonly IReadOnlyList<int> PageSizes = [10, 20, 50, 100];

    public async Task<long> VerifyCredentialsAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var user = string.IsNullOrWhiteSpace(request.Email) ? null : await users.FindByEmailAsync(request.Email.Trim()).ConfigureAwait(false);
        if (user is null || !user.Active)
        {
            throw new UnauthorizedException();
        }

        if (await users.IsLockedOutAsync(user).ConfigureAwait(false))
        {
            throw LockedOut(user);
        }

        if (!await users.CheckPasswordAsync(user, request.Password ?? string.Empty).ConfigureAwait(false))
        {
            // Conta a tentativa; ao atingir o limite (IdentityOptions.Lockout) a conta fica bloqueada.
            await users.AccessFailedAsync(user).ConfigureAwait(false);
            if (await users.IsLockedOutAsync(user).ConfigureAwait(false))
            {
                throw LockedOut(user);
            }

            throw new UnauthorizedException();
        }

        await users.ResetAccessFailedCountAsync(user).ConfigureAwait(false);
        return user.Id;
    }

    public async Task<UserProfileDto> GetProfileAsync(long userId, CancellationToken cancellationToken)
    {
        var user = await FindAsync(userId).ConfigureAwait(false);
        return new UserProfileDto(
            user.Id,
            user.Email!,
            user.FullName,
            await RoleOfAsync(user).ConfigureAwait(false),
            user.Theme,
            user.PageSize,
            user.Version);
    }

    public async Task<UserProfileDto> UpdateProfileAsync(long userId, UpdateProfileRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var user = await FindAsync(userId).ConfigureAwait(false);
        EnsureVersion(user, request.Version);

        var errors = new ValidationErrors().Required("fullName", request.FullName, AppUser.FullNameMaxLength);
        if (!Enum.IsDefined(request.Theme))
        {
            errors.Add("theme", "Tema inválido.");
        }

        if (!PageSizes.Contains(request.PageSize))
        {
            errors.Add("pageSize", $"Use {string.Join(", ", PageSizes)}.");
        }

        errors.ThrowIfAny();

        user.FullName = request.FullName.Trim();
        user.Theme = request.Theme;
        user.PageSize = request.PageSize;
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return await GetProfileAsync(userId, cancellationToken).ConfigureAwait(false);
    }

    public async Task ChangePasswordAsync(long userId, ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var user = await FindAsync(userId).ConfigureAwait(false);
        if (!await users.CheckPasswordAsync(user, request.CurrentPassword ?? string.Empty).ConfigureAwait(false))
        {
            new ValidationErrors().Add("currentPassword", "Senha atual incorreta.").ThrowIfAny();
        }

        // Trocar a senha também troca o security stamp: as outras sessões perdem o refresh.
        ThrowIfFailed(await users.ChangePasswordAsync(user, request.CurrentPassword!, request.NewPassword ?? string.Empty).ConfigureAwait(false), "newPassword");
    }

    public async Task RevokeSessionsAsync(long userId, CancellationToken cancellationToken)
    {
        var user = await FindAsync(userId).ConfigureAwait(false);
        ThrowIfFailed(await users.UpdateSecurityStampAsync(user).ConfigureAwait(false), "user");
    }

    public async Task<PagedResult<UserDto>> ListAsync(UserListQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        var page = PageRequest.From(query.Page, query.PageSize);
        var rows = UserRows();

        var pattern = SearchPattern.Prefix(query.Search);
        if (pattern is not null)
        {
            rows = rows.Where(r => EF.Functions.Like(r.User.FullName, pattern, SearchPattern.EscapeCharacter)
                || EF.Functions.Like(r.User.Email!, pattern, SearchPattern.EscapeCharacter));
        }

        if (!string.IsNullOrWhiteSpace(query.Role))
        {
            rows = rows.Where(r => r.Role == query.Role);
        }

        if (query.Active is { } active)
        {
            rows = rows.Where(r => r.User.Active == active);
        }

        var now = clock.GetUtcNow();
        var result = await rows
            .OrderBy(r => r.User.FullName)
            .ThenBy(r => r.User.Id)
            .ToPagedResultAsync(page, cancellationToken)
            .ConfigureAwait(false);
        return new PagedResult<UserDto>([.. result.Items.Select(r => ToDto(r, now))], result.Page, result.PageSize, result.TotalCount);
    }

    public async Task<UserDto> GetAsync(long id, CancellationToken cancellationToken)
    {
        var row = await UserRows().FirstOrDefaultAsync(r => r.User.Id == id, cancellationToken).ConfigureAwait(false)
            ?? throw NotFound();
        return ToDto(row, clock.GetUtcNow());
    }

    public async Task<UserDto> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var email = request.Email?.Trim().ToLowerInvariant() ?? string.Empty;
        var errors = new ValidationErrors()
            .Required("email", email, 256)
            .Required("fullName", request.FullName, AppUser.FullNameMaxLength);
        ValidateRole(errors, request.Role);
        errors.ThrowIfAny();

        if (await users.FindByEmailAsync(email).ConfigureAwait(false) is not null)
        {
            throw new ConflictException(ErrorCodes.UserEmailDuplicate, $"Já existe um usuário com o e-mail {email}.");
        }

        var user = new AppUser { UserName = email, Email = email, EmailConfirmed = true, FullName = request.FullName.Trim() };
        ThrowIfFailed(await users.CreateAsync(user, request.Password ?? string.Empty).ConfigureAwait(false), "password");
        ThrowIfFailed(await users.AddToRoleAsync(user, request.Role).ConfigureAwait(false), "role");
        return await GetAsync(user.Id, cancellationToken).ConfigureAwait(false);
    }

    public async Task<UserDto> UpdateAsync(long id, long actingUserId, UpdateUserRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var user = await FindAsync(id).ConfigureAwait(false);
        EnsureVersion(user, request.Version);

        var errors = new ValidationErrors().Required("fullName", request.FullName, AppUser.FullNameMaxLength);
        ValidateRole(errors, request.Role);
        errors.ThrowIfAny();

        // O Admin não pode se trancar fora: nem se inativar nem tirar o próprio perfil de Admin.
        if (id == actingUserId && (!request.Active || request.Role != Roles.Admin))
        {
            throw new ConflictException(ErrorCodes.UserSelfLockout, "Você não pode inativar a própria conta nem remover o próprio perfil de Admin.");
        }

        var currentRole = await RoleOfAsync(user).ConfigureAwait(false);
        if (currentRole != request.Role)
        {
            ThrowIfFailed(await users.RemoveFromRolesAsync(user, await users.GetRolesAsync(user).ConfigureAwait(false)).ConfigureAwait(false), "role");
            ThrowIfFailed(await users.AddToRoleAsync(user, request.Role).ConfigureAwait(false), "role");
        }

        var deactivated = user.Active && !request.Active;
        user.FullName = request.FullName.Trim();
        user.Active = request.Active;
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        if (deactivated || currentRole != request.Role)
        {
            // Perfil novo ou conta inativada valem já na próxima renovação da sessão.
            ThrowIfFailed(await users.UpdateSecurityStampAsync(user).ConfigureAwait(false), "user");
        }

        return await GetAsync(user.Id, cancellationToken).ConfigureAwait(false);
    }

    public async Task ResetPasswordAsync(long id, ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var user = await FindAsync(id).ConfigureAwait(false);
        var token = await users.GeneratePasswordResetTokenAsync(user).ConfigureAwait(false);
        ThrowIfFailed(await users.ResetPasswordAsync(user, token, request.NewPassword ?? string.Empty).ConfigureAwait(false), "newPassword");
        // Senha redefinida pelo Admin também destrava a conta.
        await users.SetLockoutEndDateAsync(user, null).ConfigureAwait(false);
        await users.ResetAccessFailedCountAsync(user).ConfigureAwait(false);
    }

    private IQueryable<UserRow> UserRows() =>
        from user in db.Users.AsNoTracking()
        select new UserRow
        {
            User = user,
            Role = (from link in db.UserRoles
                    join role in db.Roles on link.RoleId equals role.Id
                    where link.UserId == user.Id
                    select role.Name).FirstOrDefault() ?? Roles.Viewer,
        };

    private static UserDto ToDto(UserRow row, DateTimeOffset now) => new(
        row.User.Id,
        row.User.Email!,
        row.User.FullName,
        row.Role,
        row.User.Active,
        row.User.LockoutEnd is { } end && end > now,
        row.User.Version,
        row.User.CreatedAt,
        row.User.UpdatedAt);

    private async Task<AppUser> FindAsync(long id) =>
        await users.FindByIdAsync(id.ToString(System.Globalization.CultureInfo.InvariantCulture)).ConfigureAwait(false)
        ?? throw NotFound();

    private async Task<string> RoleOfAsync(AppUser user) =>
        (await users.GetRolesAsync(user).ConfigureAwait(false)).FirstOrDefault() ?? Roles.Viewer;

    private static void EnsureVersion(AppUser user, int version)
    {
        if (user.Version != version)
        {
            throw ConflictException.StaleVersion();
        }
    }

    private static void ValidateRole(ValidationErrors errors, string? role)
    {
        if (!Roles.IsValid(role))
        {
            errors.Add("role", $"Use {string.Join(", ", Roles.All)}.");
        }
    }

    /// <summary>Erros do Identity (senha fraca, por exemplo) viram 400 no campo informado, em pt-BR.</summary>
    private static void ThrowIfFailed(IdentityResult result, string field)
    {
        if (result.Succeeded)
        {
            return;
        }

        var errors = new ValidationErrors();
        foreach (var error in result.Errors)
        {
            errors.Add(field, IdentityMessages.Translate(error));
        }

        errors.ThrowIfAny();
    }

    private UnauthorizedException LockedOut(AppUser user)
    {
        var minutes = user.LockoutEnd is { } end
            ? Math.Max(1, (int)Math.Ceiling((end - clock.GetUtcNow()).TotalMinutes))
            : 1;
        return new UnauthorizedException(ErrorCodes.LockedOut, $"Conta bloqueada por excesso de tentativas. Tente de novo em {minutes} min.");
    }

    private static NotFoundException NotFound() => new(ErrorCodes.UserNotFound, "Usuário não encontrado.");

    private sealed class UserRow
    {
        public AppUser User { get; init; } = null!;

        public string Role { get; init; } = Roles.Viewer;
    }
}
