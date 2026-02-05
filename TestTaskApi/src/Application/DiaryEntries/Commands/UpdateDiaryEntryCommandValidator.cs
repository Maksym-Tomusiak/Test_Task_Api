using FluentValidation;

namespace Application.DiaryEntries.Commands;

public class UpdateDiaryEntryCommandValidator : AbstractValidator<UpdateDiaryEntryCommand>
{
    public UpdateDiaryEntryCommandValidator()
    {
        RuleFor(x => x.Content)
            .NotEmpty()
            .MaximumLength(500);

        RuleFor(x => x.DiaryEntryId)
            .NotEmpty();
    }
}