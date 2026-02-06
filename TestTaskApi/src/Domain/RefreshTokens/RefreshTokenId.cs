namespace Domain.RefreshTokens;

public record RefreshTokenId(Guid Value)
{
    public static RefreshTokenId Empty() => new(Guid.Empty);
    public static RefreshTokenId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString();
}