using FluentValidation;

namespace Application.DiaryEntries.Queries;

public class GetDiaryEntriesByUserPaginatedQueryValidator : AbstractValidator<GetDiaryEntriesByUserPaginatedQuery>
{
    public GetDiaryEntriesByUserPaginatedQueryValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.PageNumber).GreaterThan(0);
        RuleFor(x => x.PageSize).GreaterThan(0).LessThanOrEqualTo(100);
        RuleFor(x => x.SearchTerm).MaximumLength(255);
        RuleFor(x => x.SortBy).MaximumLength(255);
    }
}
