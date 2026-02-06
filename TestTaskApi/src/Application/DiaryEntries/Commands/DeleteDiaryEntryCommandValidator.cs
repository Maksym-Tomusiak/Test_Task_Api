using FluentValidation;

namespace Application.DiaryEntries.Commands;

public class DeleteDiaryEntryCommandValidator : AbstractValidator<DeleteDiaryEntryCommand>
{
    public DeleteDiaryEntryCommandValidator()
    {
        RuleFor(x => x.DiaryEntryId).NotEmpty();
    }
}