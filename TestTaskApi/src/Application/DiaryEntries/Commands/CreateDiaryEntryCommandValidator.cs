using FluentValidation;

namespace Application.DiaryEntries.Commands;

public class CreateDiaryEntryCommandValidator : AbstractValidator<CreateDiaryEntryCommand>
{
    public CreateDiaryEntryCommandValidator()
    {
        RuleFor(x => x.Content)
            .NotEmpty()
            .MaximumLength(500);
    }
}