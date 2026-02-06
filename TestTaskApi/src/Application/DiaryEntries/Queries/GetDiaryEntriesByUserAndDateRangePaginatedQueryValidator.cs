using FluentValidation;

namespace Application.DiaryEntries.Queries;

public class GetDiaryEntriesByUserAndDateRangePaginatedQueryValidator : AbstractValidator<GetDiaryEntriesByUserAndDateRangePaginatedQuery>
{
    public GetDiaryEntriesByUserAndDateRangePaginatedQueryValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.StartDate).NotEmpty();
        RuleFor(x => x.EndDate).NotEmpty().GreaterThanOrEqualTo(x => x.StartDate)
            .WithMessage("End date must be greater than or equal to start date");
        RuleFor(x => x.PageNumber).GreaterThan(0);
        RuleFor(x => x.PageSize).GreaterThan(0).LessThanOrEqualTo(100);
        RuleFor(x => x.SearchTerm).MaximumLength(255);
        RuleFor(x => x.SortBy).MaximumLength(255);
    }
}
