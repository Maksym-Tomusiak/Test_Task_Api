using FluentValidation;

namespace Application.Users.Commands;

public class InviteUserCommandValidator: AbstractValidator<InviteUserCommand>
{
    public InviteUserCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
    }
}