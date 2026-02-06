namespace Application.Users.Exceptions;

public class UserException(Guid id, string message, Exception? innerException = null)
    : Exception(message, innerException)
{
    public Guid Id { get; } = id;
}

public class UserNotFoundException(Guid id) 
    : UserException(id, $"User under id: {id} not found!");

public class UserIdNotFoundException() 
    : UserException(Guid.Empty, $"User id not found!");

public class UserRoleNotFoundException(Guid id)
    : UserException(id, $"User role under id: {id} not found!");

public class UserWithNameAlreadyExistsException(Guid id) 
    : UserException(id, $"User under such user name already exists!");

public class InvalidCredentialsException() 
    : UserException(Guid.Empty, $"Invalid credentials!");

public class UserUnauthorizedAccessException(string message) 
    : UserException(Guid.Empty, message);

public class UserAlreadyInvitedException(string email) 
    : UserException(Guid.Empty, $"User with email: {email} is already invited!");

public class CaptchaExpiredException() 
    : UserException(Guid.Empty, "Captcha has expired!");

public class CaptchaIncorrectException() 
    : UserException(Guid.Empty, "Captcha  is incorrect!");

public class InviteNotFoundException(Guid id) 
    : UserException(id, $"Invite with id {id} was not found.");
public class InviteAlreadyUsedException(Guid id) 
    : UserException(Guid.Empty, $"Invite with id {id} has already been used.");
public class InviteExpiredException(Guid id) 
    : UserException(Guid.Empty, $"Invite with id {id} has expired.");
public class InviteEmailMismatchException() 
    : UserException(Guid.Empty, "The email provided does not match the invite email.");
public class UserWithEmailAlreadyExistsException(string email) 
    : UserException(Guid.Empty, $"User with email {email} already exists.");

public class UserRestoreFailedException(Guid id, List<string> errors)
    : UserException(id, $"Restoring User under id: {id} failed! Errors: {string.Join(", ", errors)}");

public class UserUnknownException(Guid id, Exception innerException)
    : UserException(id, $"Unknown exception for the User under id: {id}!", innerException);