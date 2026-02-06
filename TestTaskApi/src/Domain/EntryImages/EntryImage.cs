using Domain.DiaryEntries;

namespace Domain.EntryImages;

public class EntryImage
{
    public EntryImageId Id { get; set; }
    public DiaryEntryId EntryId { get; set; }
    public DiaryEntry? Entry { get; set; }
    public byte[] ImageData { get; set; }
    public string MimeType { get; set; }
    
    private EntryImage(EntryImageId id, DiaryEntryId entryId, byte[] imageData, string mimeType)
    {
        Id = id;
        EntryId = entryId;
        ImageData = imageData;
        MimeType = mimeType;
    }
    
    public static EntryImage New(DiaryEntryId entryId, byte[] imageData, string mimeType) =>
        new(EntryImageId.New(), entryId, imageData, mimeType);

    public void UpdateImage(byte[] imageData, string mimeType)
    {
        ImageData = imageData;
        MimeType = mimeType;
    }
}