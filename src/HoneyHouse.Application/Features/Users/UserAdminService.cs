using FluentValidation;
using HoneyHouse.Application.Common;
using HoneyHouse.Application.Interfaces.Repositories;
using HoneyHouse.Domain.Common;
using HoneyHouse.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace HoneyHouse.Application.Features.Users;

public sealed record AdminUserDto(string Id, string FullName, string Email, string? PhoneNumber, IReadOnlyList<string> Roles,
    bool IsActive, bool IsLockedOut, string PreferredLanguage, DateTime CreatedAt);

public sealed record UserQuery(string? Search = null, string? Role = null, int Page = 1, int PageSize = 20);
public sealed record UpdateRolesRequest(IReadOnlyList<string> Roles);
public sealed record UpdateUserStatusRequest(bool IsActive);
public sealed record CreateStaffRequest(string FullName, string Email, string PhoneNumber, string Password, IReadOnlyList<string> Roles);

public sealed class UpdateRolesRequestValidator : AbstractValidator<UpdateRolesRequest>
{
    public UpdateRolesRequestValidator() => RuleFor(x => x.Roles).NotEmpty();
}

public sealed class CreateStaffRequestValidator : AbstractValidator<CreateStaffRequest>
{
    public CreateStaffRequestValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.PhoneNumber).NotEmpty().EgyptianMobile();
        RuleFor(x => x.Password).StrongPassword();
        RuleFor(x => x.Roles).NotEmpty();
    }
}

public interface IUserAdminService
{
    Task<PagedResult<AdminUserDto>> SearchAsync(UserQuery query, CancellationToken ct = default);
    Task<Result<AdminUserDto>> GetAsync(string id, CancellationToken ct = default);
    Task<Result<AdminUserDto>> CreateStaffAsync(CreateStaffRequest request, CancellationToken ct = default);
    Task<Result<AdminUserDto>> UpdateRolesAsync(string actingUserId, string id, UpdateRolesRequest request, CancellationToken ct = default);
    Task<Result<AdminUserDto>> UpdateStatusAsync(string actingUserId, string id, UpdateUserStatusRequest request, CancellationToken ct = default);
}

public sealed class UserAdminService(UserManager<ApplicationUser> users, IUnitOfWork uow, TimeProvider clock) : IUserAdminService
{
    public async Task<PagedResult<AdminUserDto>> SearchAsync(UserQuery q, CancellationToken ct = default)
    {
        var paging = new PagedRequest { Page = q.Page, PageSize = q.PageSize };
        IEnumerable<ApplicationUser> source = q.Role is { Length: > 0 } role && Roles.All.Contains(role)
            ? await users.GetUsersInRoleAsync(role)
            : users.Users.ToList();

        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            var term = q.Search.Trim();
            source = source.Where(u => u.FullName.Contains(term, StringComparison.OrdinalIgnoreCase)
                                       || (u.Email ?? "").Contains(term, StringComparison.OrdinalIgnoreCase)
                                       || (u.PhoneNumber ?? "").Contains(term));
        }
        var list = source.OrderByDescending(u => u.CreatedAt).ToList();
        var page = new List<AdminUserDto>();
        foreach (var u in list.Skip(paging.Skip).Take(paging.PageSize)) page.Add(await ToDtoAsync(u));
        return new PagedResult<AdminUserDto>(page, paging.Page, paging.PageSize, list.Count);
    }

    public async Task<Result<AdminUserDto>> GetAsync(string id, CancellationToken ct = default) =>
        await users.FindByIdAsync(id) is { } u ? await ToDtoAsync(u) : Error.NotFound("user_not_found");

    public async Task<Result<AdminUserDto>> CreateStaffAsync(CreateStaffRequest r, CancellationToken ct = default)
    {
        var invalid = r.Roles.FirstOrDefault(x => !Roles.All.Contains(x));
        if (invalid is not null) return Error.Validation("role_invalid", "roles", invalid);
        if (await users.FindByEmailAsync(r.Email) is not null) return Error.Conflict("email_taken");

        var user = new ApplicationUser
        {
            UserName = r.Email.Trim().ToLowerInvariant(), Email = r.Email.Trim().ToLowerInvariant(), EmailConfirmed = true,
            FullName = r.FullName.Trim(), PhoneNumber = r.PhoneNumber
        };
        var created = await users.CreateAsync(user, r.Password);
        if (!created.Succeeded) return Error.Business("identity_error", created.Errors.First().Description);
        await users.AddToRolesAsync(user, r.Roles.Distinct());
        return await ToDtoAsync(user);
    }

    public async Task<Result<AdminUserDto>> UpdateRolesAsync(string actingUserId, string id, UpdateRolesRequest r, CancellationToken ct = default)
    {
        var invalid = r.Roles.FirstOrDefault(x => !Roles.All.Contains(x));
        if (invalid is not null) return Error.Validation("role_invalid", "roles", invalid);
        var user = await users.FindByIdAsync(id);
        if (user is null) return Error.NotFound("user_not_found");
        if (id == actingUserId && !r.Roles.Contains(Roles.Admin)) return Error.Business("cannot_modify_self");

        var current = await users.GetRolesAsync(user);
        await users.RemoveFromRolesAsync(user, current.Except(r.Roles));
        await users.AddToRolesAsync(user, r.Roles.Distinct().Except(current));
        await RevokeSessionsAsync(user.Id, ct); // new roles take effect on next sign-in
        return await ToDtoAsync(user);
    }

    public async Task<Result<AdminUserDto>> UpdateStatusAsync(string actingUserId, string id, UpdateUserStatusRequest r, CancellationToken ct = default)
    {
        var user = await users.FindByIdAsync(id);
        if (user is null) return Error.NotFound("user_not_found");
        if (id == actingUserId && !r.IsActive) return Error.Business("cannot_modify_self");

        user.IsActive = r.IsActive;
        await users.UpdateAsync(user);
        if (!r.IsActive) await RevokeSessionsAsync(user.Id, ct);
        return await ToDtoAsync(user);
    }

    private async Task RevokeSessionsAsync(string userId, CancellationToken ct)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        var tokens = await uow.Repository<RefreshToken>().ListAsync(t => t.UserId == userId && t.RevokedAt == null, ct);
        foreach (var t in tokens) t.RevokedAt = now;
        await uow.SaveChangesAsync(ct);
    }

    private async Task<AdminUserDto> ToDtoAsync(ApplicationUser u) => new(u.Id, u.FullName, u.Email ?? "", u.PhoneNumber,
        (await users.GetRolesAsync(u)).ToList(), u.IsActive, await users.IsLockedOutAsync(u), u.PreferredLanguage, u.CreatedAt);
}
