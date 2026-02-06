namespace Domain.DiaryEntries;

public record DiaryEntryId(Guid Value)
{
    public static DiaryEntryId New() => new(Guid.NewGuid());
    public static DiaryEntryId Empty() => new(Guid.Empty);
    public override string ToString() => Value.ToString();
}