namespace Application.DiaryEntries.Exceptions;

public class DiaryEntryException(Guid id, string message, Exception? innerException = null)
    : Exception(message, innerException)
{
    public Guid Id { get; } = id;
}

public class UnauthorizedDiaryEntryAccessException() 
    : DiaryEntryException(Guid.Empty, "Unauthorized access to Diary Entry!");

public class DiaryEntryNotFoundException(Guid id) 
    : DiaryEntryException(id, $"Diary Entry under id: {id} not found!");

public class DiaryEntryEntryCannotBeDeletedException(Guid id) 
    : DiaryEntryException(id, $"Diary Entry under id: {id} can't be deleted!");
    
public class DiaryEntryEntryUnknownException(Guid id, Exception innerException)
    : DiaryEntryException(id, $"Unknown exception for the Diary Entry under id: {id}!", innerException);