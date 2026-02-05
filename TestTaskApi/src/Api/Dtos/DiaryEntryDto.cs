using Domain.DiaryEntries;

namespace Api.Dtos;

public record DiaryEntryDto(
    Guid Id,
    string Content,
    DateTime EntryDate,
    bool HasImage)
{
    public static DiaryEntryDto FromDomainModel(DiaryEntry entry, string decryptedContent)
        => new(entry.Id.Value, decryptedContent, entry.EntryDate, entry.HasImage);
}

public record CreateDiaryEntryDto(
    string Content);

public record UpdateDiaryEntryDto(
    string Content);
