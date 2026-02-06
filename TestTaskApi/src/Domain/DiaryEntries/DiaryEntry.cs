using Domain.Users;

namespace Domain.DiaryEntries;

public class DiaryEntry
{
    public DiaryEntryId Id { get; set; }
    public Guid UserId { get; set; }
    public User? User { get; set; }
    public byte[] EncryptedContent { get; set; }
    public byte[] InitializationVector { get; set; }
    public DateTime EntryDate { get; set; }
    public bool HasImage { get; set; }
    
    private DiaryEntry(DiaryEntryId id, Guid userId, byte[] encryptedContent, byte[] initializationVector, DateTime entryDate, bool hasImage = false)
    {
        Id = id;
        UserId = userId;
        EncryptedContent = encryptedContent;
        InitializationVector = initializationVector;
        EntryDate = entryDate;
        HasImage = hasImage;
    }
    
    public static DiaryEntry New(Guid userId, byte[] encryptedContent, byte[] initializationVector, DateTime entryDate, bool hasImage = false) =>
        new(DiaryEntryId.New(), userId, encryptedContent, initializationVector, entryDate, hasImage);

    public void UpdateContent(byte[] encryptedContent, byte[] initializationVector)
    {
        EncryptedContent = encryptedContent;
        InitializationVector = initializationVector;
    }
    
    public void SetHasImage(bool hasImage) => HasImage = hasImage;
}