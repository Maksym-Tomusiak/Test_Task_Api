using FluentValidation;

namespace Application.Users.Commands;

public class RefreshUserCommandValidator : AbstractValidator<RefreshUserCommand>
{
    public RefreshUserCommandValidator()
    {
        RuleFor(x => x.RefreshToken).NotEmpty();
    }
}