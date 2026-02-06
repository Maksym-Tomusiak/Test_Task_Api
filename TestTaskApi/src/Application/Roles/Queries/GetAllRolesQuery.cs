using Domain.Roles;
using Microsoft.AspNetCore.Identity;

namespace Application.Roles.Queries;

public record GetAllRolesQuery;

public static class GetAllRolesHandler
{
    public static Task<IEnumerable<Role>> Handle(GetAllRolesQuery request, RoleManager<Role> roleManager)
        =>
            Task.FromResult<IEnumerable<Role>>(roleManager.Roles.ToList());
}