using API.Models;
using FluentValidation;

namespace API.Validators;

public class ArticleToUpsertValidator : AbstractValidator<ArticleToUpsertDto>
{
    public ArticleToUpsertValidator()
    {
        RuleFor(article => article.Title)
            .NotEmpty()
            .WithMessage("Title cannot be empty");
        RuleFor(article => article.Content)
            .NotEmpty()
            .WithMessage("Content cannot be empty");
    }
}