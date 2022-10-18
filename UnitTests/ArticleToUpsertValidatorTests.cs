using API.Models;
using API.Validators;
using FluentValidation.TestHelper;

namespace UnitTests;

public class ArticleToUpsertValidatorTests
{
    private readonly ArticleToUpsertValidator _validator;

    public ArticleToUpsertValidatorTests()
    {
        _validator = new ArticleToUpsertValidator();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void RuleForTitle_WhenTitleIsNullOrEmpty(string title)
    {
        var articleToUpsertDto = new ArticleToUpsertDto
        {
            Title = title,
            Content = "this is content"
        };
        var result = _validator.TestValidate(articleToUpsertDto);
        result
            .ShouldHaveValidationErrorFor(dto => dto.Title)
            .WithErrorMessage("Title cannot be empty");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void RuleForContent_WhenContentIsNullOrEmpty(string content)
    {
        var articleToUpsertDto = new ArticleToUpsertDto
        {
            Title = "this is title",
            Content = content
        };
        var result = _validator.TestValidate(articleToUpsertDto);
        result
            .ShouldHaveValidationErrorFor(dto => dto.Content)
            .WithErrorMessage("Content cannot be empty");
    }
}