using API.Models;
using FluentValidation;

namespace API.Validators;

public class CommentToInsertValidator : AbstractValidator<CommentToInsertDto>
{
    public CommentToInsertValidator()
    {
        RuleFor(comment => comment.content)
            .NotEmpty()
            .WithMessage("Comment cannot be empty");
    }
}