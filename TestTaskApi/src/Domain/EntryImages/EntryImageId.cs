namespace Domain.EntryImages;

public record EntryImageId(Guid Value)
{
    public static EntryImageId New() => new(Guid.NewGuid());
    public static EntryImageId Empty() => new(Guid.Empty);
    public override string ToString() => Value.ToString();
}