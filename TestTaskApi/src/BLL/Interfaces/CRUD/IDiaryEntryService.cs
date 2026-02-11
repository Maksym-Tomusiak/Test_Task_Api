using BLL.Modules.Exceptions;
using Domain.DiaryEntries;
using LanguageExt;

namespace BLL.Interfaces.CRUD;

public record DiaryEntryWithImageId(DiaryEntry Entry, Guid? ImageId);

public record PaginatedDiaryEntries(
    IReadOnlyList<DiaryEntryWithImageId> Items,
    int TotalCount,
    int PageNumber,
    int PageSize,
    int TotalPages);

public interface IDiaryEntryService
{
    Task<PaginatedDiaryEntries> GetAllByUserIdAsync(
        Guid userId,
        int pageNumber,
        int pageSize,
        string? searchTerm,
        DateTime? startDate,
        DateTime? endDate,
        CancellationToken cancellationToken);

    Task<Option<DiaryEntryWithImageId>> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<Either<DiaryEntryException, DiaryEntry>> CreateAsync(
        string content,
        Stream? imageStream,
        CancellationToken cancellationToken);

    Task<Either<DiaryEntryException, DiaryEntry>> UpdateAsync(
        Guid diaryEntryId,
        string content,
        Stream? imageStream,
        bool deleteCurrentImage,
        CancellationToken cancellationToken);

    Task<Either<DiaryEntryException, DiaryEntry>> DeleteAsync(
        Guid diaryEntryId,
        CancellationToken cancellationToken);
}
