using BLL.Dtos;
using FluentValidation;

namespace BLL.Modules.Validators;

public class CreateDiaryEntryDtoValidator : AbstractValidator<CreateDiaryEntryDto>
{
    public CreateDiaryEntryDtoValidator()
    {
        RuleFor(x => x.Content)
            .NotEmpty()
            .WithMessage("Content is required")
            .MaximumLength(500)
            .WithMessage("Content cannot exceed 500 characters");
    }
}
