using HoneyHouse.Application.Common;
using HoneyHouse.Application.Features.Users;

namespace HoneyHouse.Admin.Models;

public sealed class UserListViewModel
{
    public required PagedResult<AdminUserDto> Result { get; init; }
    public string? Search { get; init; }
    public string? Role { get; init; }
}

public sealed class StaffForm
{
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";
    public string PhoneNumber { get; set; } = "";
    public string Password { get; set; } = "";
    public List<string> Roles { get; set; } = [HoneyHouse.Domain.Common.Roles.Manager];

    public CreateStaffRequest ToRequest() => new(FullName?.Trim() ?? "", Email?.Trim() ?? "", PhoneNumber?.Trim() ?? "", Password ?? "", Roles);
}

public sealed class UserDetailsViewModel
{
    public required AdminUserDto User { get; init; }
    public bool IsSelf { get; init; }
}
