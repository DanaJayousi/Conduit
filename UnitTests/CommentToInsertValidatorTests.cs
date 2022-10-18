using API.Models;
using API.Validators;
using FluentValidation.TestHelper;

namespace UnitTests;

public class CommentToInsertValidatorTests
{
    private readonly CommentToInsertValidator _validator;

    public CommentToInsertValidatorTests()
    {
        _validator = new CommentToInsertValidator();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void RuleForContent_WhenContentIsNullOrEmpty(string Content)
    {
        var commentToInsertDto = new CommentToInsertDto
        {
            content = Content
        };
        var result = _validator.TestValidate(commentToInsertDto);
        result
            .ShouldHaveValidationErrorFor(dto => dto.content)
            .WithErrorMessage("Comment cannot be empty");
    }
}