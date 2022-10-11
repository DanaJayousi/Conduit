using API.Models;
using FluentValidation;

namespace API.Validators;

public class UserForUpsertValidator : AbstractValidator<UserForUpsertDto>
{
    public UserForUpsertValidator()
    {
        RuleFor(user => user.Email)
            .NotEmpty()
            .WithMessage("Email address is required")
            .EmailAddress()
            .WithMessage("Email is invalid");
        RuleFor(user => user.Password)
            .MinimumLength(6)
            .WithMessage("Password must be 6 or more characters");
        RuleFor(user => user.FirstName)
            .NotEmpty()
            .WithMessage("First name cannot be empty");
        RuleFor(user => user.LastName)
            .NotEmpty()
            .WithMessage("Last name cannot be empty");
    }
}