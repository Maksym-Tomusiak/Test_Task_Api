using Application.Common.Models;
using Domain.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Application.Users.Queries;

public record GetAllUsersPaginatedQuery(
    int PageNumber = 1,
    int PageSize = 10,
    string? SearchTerm = null,
    string? SortBy = null,
    bool SortDescending = false);

public static class GetAllUsersPaginatedQueryHandler
{
    public static async Task<PaginatedResult<(User, IReadOnlyList<string>)>> Handle(
        GetAllUsersPaginatedQuery request,
        UserManager<User> userManager,
        CancellationToken cancellationToken)
    {
        var paginationParameters = new PaginationParameters(
            request.PageNumber,
            request.PageSize,
            request.SearchTerm,
            request.SortBy,
            request.SortDescending);
        
        var query = userManager.Users.AsNoTracking();
            
        if (!string.IsNullOrWhiteSpace(paginationParameters.SearchTerm))
        {
            var searchTerm = paginationParameters.SearchTerm.ToLower();
            query = query.Where(application =>
                application!.UserName.ToLower().Contains(searchTerm) ||
                application!.Email.ToLower().Contains(searchTerm));
        }

        if (!string.IsNullOrWhiteSpace(paginationParameters.SortBy))
        {
            query = paginationParameters.SortBy?.ToLower() switch
            {
                "username" => paginationParameters.SortDescending
                    ? query.OrderByDescending(u => u.UserName)
                    : query.OrderBy(u => u.UserName),
                "email" => paginationParameters.SortDescending
                    ? query.OrderByDescending(u => u.Email)
                    : query.OrderBy(u => u.Email),
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((paginationParameters.PageNumber - 1) * paginationParameters.PageSize)
            .Take(paginationParameters.PageSize)
            .ToListAsync(cancellationToken);

        var totalPages = (int)Math.Ceiling(totalCount / (double)paginationParameters.PageSize);

        IEnumerable<(User, IReadOnlyList<string>)> resultItems = [];
        foreach (var item in items)
        {
            var roles = await userManager.GetRolesAsync(item);
            resultItems = resultItems.Append((item, roles.ToList()));
        }
        
        return new PaginatedResult<(User, IReadOnlyList<string>)>(
            resultItems,
            totalCount,
            paginationParameters.PageNumber,
            paginationParameters.PageSize,
            totalPages);
    }
}