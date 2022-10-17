using API.Models;
using API.Validators;
using FluentValidation.TestHelper;

namespace UnitTests;

public class UserForUpsertValidatorTests
{
    private readonly UserForUpsertValidator _validator;

    public UserForUpsertValidatorTests()
    {
        _validator = new UserForUpsertValidator();
    }

    [Fact]
    public void RuleForEmail_WhenEmailAddressIsNull()
    {
        var userForUpsertDto = new UserForUpsertDto
        {
            Email = null,
            Password = "123456",
            FirstName = "FN",
            LastName = "LN"
        };
        var result = _validator.TestValidate(userForUpsertDto);
        result
            .ShouldHaveValidationErrorFor(dto => dto.Email)
            .WithErrorMessage("Email address is required");
    }

    [Theory]
    [InlineData("wrongEmail")]
    [InlineData("@wrongEmail")]
    [InlineData("wrongEmail@")]
    public void RuleForEmail_WhenEmailAddressFormatIsInvalid(string email)
    {
        var userForUpsertDto = new UserForUpsertDto
        {
            Email = email,
            Password = "123456",
            FirstName = "FN",
            LastName = "LN"
        };
        var result = _validator.TestValidate(userForUpsertDto);
        result
            .ShouldHaveValidationErrorFor(dto => dto.Email)
            .WithErrorMessage("Email is invalid");
    }

    [Theory]
    [InlineData("")]
    [InlineData("123")]
    [InlineData("12345")]
    public void RuleForPassword_WhenPasswordIsTooShort(string password)
    {
        var userForUpsertDto = new UserForUpsertDto
        {
            Email = "valid@email",
            Password = password,
            FirstName = "FN",
            LastName = "LN"
        };
        var result = _validator.TestValidate(userForUpsertDto);
        result
            .ShouldHaveValidationErrorFor(dto => dto.Password)
            .WithErrorMessage("Password must be 6 or more characters");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void RuleForFirstName_WhenFirstNameIsNull(string firstName)
    {
        var userForUpsertDto = new UserForUpsertDto
        {
            Email = "valid@email",
            Password = "123456",
            FirstName = firstName,
            LastName = "LN"
        };
        var result = _validator.TestValidate(userForUpsertDto);
        result
            .ShouldHaveValidationErrorFor(dto => dto.FirstName)
            .WithErrorMessage("First name cannot be empty");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void RuleForLastName_WhenLastNameIsNullOrEmpty(string lastName)
    {
        var userForUpsertDto = new UserForUpsertDto
        {
            Email = "valid@email",
            Password = "123456",
            FirstName = "FN",
            LastName = lastName
        };
        var result = _validator.TestValidate(userForUpsertDto);
        result
            .ShouldHaveValidationErrorFor(dto => dto.LastName)
            .WithErrorMessage("Last name cannot be empty");
    }
}