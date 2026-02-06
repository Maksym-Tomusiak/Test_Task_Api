using Application.Common.Interfaces.Queries;
using Application.Common.Interfaces.Services;
using Application.Common.Models;
using Domain.DiaryEntries;

namespace Application.DiaryEntries.Queries;

public record GetAllDiaryEntriesByUserIdQuery(
    Guid UserId, 
    int PageNumber = 1, 
    int PageSize = 5, 
    string? SearchTerm = null,
    DateTime? StartDate = null,
    DateTime? EndDate = null);

public record DiaryEntryWithImageId(DiaryEntry Entry, Guid? ImageId);

public record PaginatedDiaryEntries(
    IReadOnlyList<DiaryEntryWithImageId> Items,
    int TotalCount,
    int PageNumber,
    int PageSize,
    int TotalPages);

public static class GetAllDiaryEntriesByUserIdHandler
{
    public static async Task<PaginatedDiaryEntries> Handle(
        GetAllDiaryEntriesByUserIdQuery request, 
        IDiaryEntryQueries diaryQueries,
        IEntryImageQueries imageQueries,
        ICryptoService cryptoService,
        CancellationToken ct)
    {
        // Fetch all entries for the user
        var allEntries = await diaryQueries.GetAllByUserId(request.UserId, ct);
        
        // Filter by date range if provided
        var filteredEntries = allEntries;
        if (request.StartDate.HasValue)
        {
            filteredEntries = filteredEntries.Where(e => e.EntryDate >= request.StartDate.Value).ToList();
        }
        if (request.EndDate.HasValue)
        {
            filteredEntries = filteredEntries.Where(e => e.EntryDate <= request.EndDate.Value).ToList();
        }
        
        // Filter by search term on decrypted content
        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            filteredEntries = filteredEntries
                .Where(entry =>
                {
                    var decryptedContent = cryptoService.Decrypt(entry.EncryptedContent, entry.InitializationVector);
                    return decryptedContent.Contains(request.SearchTerm, StringComparison.OrdinalIgnoreCase);
                })
                .ToList();
        }
        
        var totalCount = filteredEntries.Count;
        var totalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize);
        
        // Apply pagination
        var paginatedEntries = filteredEntries
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();
        
        // Fetch image IDs for paginated results
        var result = new List<DiaryEntryWithImageId>();
        foreach (var entry in paginatedEntries)
        {
            Guid? imageId = null;
            if (entry.HasImage)
            {
                var imageOption = await imageQueries.GetByEntryId(entry.Id, ct);
                imageId = imageOption.Match(
                    img => (Guid?)img.Id.Value,
                    () => null
                );
            }
            
            result.Add(new DiaryEntryWithImageId(entry, imageId));
        }
        
        return new PaginatedDiaryEntries(
            result,
            totalCount,
            request.PageNumber,
            request.PageSize,
            totalPages);
    }
}
