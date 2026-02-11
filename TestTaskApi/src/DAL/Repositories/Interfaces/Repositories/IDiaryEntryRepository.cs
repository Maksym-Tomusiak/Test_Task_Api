using Domain.DiaryEntries;

namespace DAL.Repositories.Interfaces.Repositories;

public interface IDiaryEntryRepository
{
    Task<DiaryEntry> Add(DiaryEntry diaryEntry, CancellationToken cancellationToken);
    Task<DiaryEntry> Update(DiaryEntry diaryEntry, CancellationToken cancellationToken);
    Task<DiaryEntry> Delete(DiaryEntry diaryEntry, CancellationToken cancellationToken);
}