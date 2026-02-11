using Domain.DiaryEntries;

namespace BLL.Dtos;

public record DiaryEntryDto(
    Guid Id,
    string Content,
    DateTime EntryDate,
    bool HasImage,
    Guid? ImageId)
{
    public static DiaryEntryDto FromDomainModel(DiaryEntry entry, string decryptedContent, Guid? imageId = null)
        => new(entry.Id.Value, decryptedContent, entry.EntryDate, entry.HasImage, imageId);
}

public record CreateDiaryEntryDto(
    string Content);

public record UpdateDiaryEntryDto(
    string Content);

public record PaginatedDiaryEntriesDto(
    IEnumerable<DiaryEntryDto> Items,
    int TotalCount,
    int PageNumber,
    int PageSize,
    int TotalPages);
