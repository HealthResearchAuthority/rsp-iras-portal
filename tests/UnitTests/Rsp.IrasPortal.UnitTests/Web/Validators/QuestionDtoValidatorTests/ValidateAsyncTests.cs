using FluentValidation;
using FluentValidation.TestHelper;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Application.DTOs;
using Rsp.IrasPortal.Web.Validators.QuestionSet;

namespace Rsp.IrasPortal.UnitTests.Web.Validators.QuestionDtoValidatorTests;

public class ValidateAsyncTests : TestServiceBase<QuestionDtoValidator>
{
    private static ValidationContext<QuestionDto> CreateValidationContext(QuestionDto model)
    {
        var context = new ValidationContext<QuestionDto>(model);
        context.RootContextData["questionDtos"] = new List<QuestionDto> { model };
        return context;
    }

    [Fact]
    public async Task ShouldRejectQuestionDtoWithEmptyQuestionId()
    {
        // Arrange
        var model = new QuestionDto
        {
            QuestionId = string.Empty,
            Category = "Test Category",
            QuestionText = "Test Question",
            QuestionType = "Test Type"
        };

        // Act
        var result = await Sut.TestValidateAsync(CreateValidationContext(model));

        // Assert
        result
            .ShouldHaveValidationErrorFor(x => x.QuestionId)
            .WithErrorMessage($"{ModuleColumns.QuestionId} column must contain a value");
    }

    [Fact]
    public async Task ShouldRejectQuestionDtoWithEmptyCategory()
    {
        // Arrange
        var model = new QuestionDto
        {
            QuestionId = "IQT001",
            Category = string.Empty,
            QuestionText = "Test Question",
            QuestionType = "Test Type"
        };

        // Act
        var result = await Sut.TestValidateAsync(CreateValidationContext(model));

        // Assert
        result
            .ShouldHaveValidationErrorFor(x => x.Category)
            .WithErrorMessage($"Question IQT001: '{ModuleColumns.Category}' column must contain a value");
    }

    [Fact]
    public async Task ShouldRejectQuestionDtoWithEmptyQuestionText()
    {
        // Arrange
        var model = new QuestionDto
        {
            QuestionId = "IQT001",
            Category = "Test Category",
            QuestionText = string.Empty,
            QuestionType = "Test Type"
        };

        // Act
        var result = await Sut.TestValidateAsync(CreateValidationContext(model));

        // Assert
        result
            .ShouldHaveValidationErrorFor(x => x.QuestionText)
            .WithErrorMessage($"Question IQT001: '{ModuleColumns.QuestionText}' column must contain a value");
    }

    [Fact]
    public async Task ShouldRejectQuestionDtoWithEmptyQuestionType()
    {
        // Arrange
        var model = new QuestionDto
        {
            QuestionId = "IQT001",
            Category = "Test Category",
            QuestionText = "Test Question",
            QuestionType = string.Empty
        };

        // Act
        var result = await Sut.TestValidateAsync(CreateValidationContext(model));

        // Assert
        result
            .ShouldHaveValidationErrorFor(x => x.QuestionType)
            .WithErrorMessage($"Question IQT001: '{ModuleColumns.QuestionType}' column must contain a value");
    }

    [Theory]
    [InlineData("IQT001")]
    [InlineData("IQA001")]
    [InlineData("IQG001")]
    public async Task ShouldAcceptQuestionIdStartingWithValidPrefix(string questionId)
    {
        // Arrange
        var model = new QuestionDto
        {
            QuestionId = questionId,
            Category = "Test Category",
            QuestionText = "Test Question",
            QuestionType = "Test Type"
        };

        // Act
        var result = await Sut.TestValidateAsync(CreateValidationContext(model));

        // Assert
        result
            .ShouldNotHaveValidationErrorFor(x => x.QuestionId);
    }

    [Fact]
    public async Task ShouldRejectQuestionDtoWithInvalidQuestionIdPrefix()
    {
        // Arrange
        var model = new QuestionDto
        {
            QuestionId = "INV001",
            Category = "Test Category",
            QuestionText = "Test Question",
            QuestionType = "Test Type"
        };

        // Act
        var result = await Sut.TestValidateAsync(CreateValidationContext(model));

        // Assert
        result
            .ShouldHaveValidationErrorFor(x => x.QuestionId)
            .WithErrorMessage("Question IDs must start with 'IQT', 'IQA', or 'IQG'");
    }

    [Theory]
    [InlineData("IQA001")]
    [InlineData("IQG001")]
    public async Task ShouldRejectQuestionDtoWithEmptySectionIdForIqaAndIqgQuestions(string questionId)
    {
        // Arrange
        var model = new QuestionDto
        {
            QuestionId = questionId,
            Category = "Test Category",
            QuestionText = "Test Question",
            QuestionType = "Test Type",
            SectionId = string.Empty,
            Sequence = 1,
            Heading = "Test Heading",
            DataType = "Test Data Type"
        };

        // Act
        var result = await Sut.TestValidateAsync(CreateValidationContext(model));

        // Assert
        result
            .ShouldHaveValidationErrorFor(x => x.SectionId)
            .WithErrorMessage($"Question {questionId}: '{ModuleColumns.Section}' column must contain a value");
    }

    [Theory]
    [InlineData("IQA001", 0)]
    [InlineData("IQG001", -1)]
    public async Task ShouldRejectQuestionDtoWithNonPositiveSequenceForIqaAndIqgQuestions(string questionId, int sequence)
    {
        // Arrange
        var model = new QuestionDto
        {
            QuestionId = questionId,
            Category = "Test Category",
            QuestionText = "Test Question",
            QuestionType = "Test Type",
            SectionId = "Test Section",
            Sequence = sequence,
            Heading = "Test Heading",
            DataType = "Test Data Type"
        };

        // Act
        var result = await Sut.TestValidateAsync(CreateValidationContext(model));

        // Assert
        result
            .ShouldHaveValidationErrorFor(x => x.Sequence)
            .WithErrorMessage($"Question {questionId}: '{ModuleColumns.Sequence}' column must contain an integer greater than 0");
    }

    [Theory]
    [InlineData("IQA001")]
    [InlineData("IQG001")]
    public async Task ShouldRejectQuestionDtoWithEmptyDataTypeForIqaAndIqgQuestions(string questionId)
    {
        // Arrange
        var model = new QuestionDto
        {
            QuestionId = questionId,
            Category = "Test Category",
            QuestionText = "Test Question",
            QuestionType = "Test Type",
            SectionId = "Test Section",
            Sequence = 1,
            Heading = "Test Heading",
            DataType = string.Empty
        };

        // Act
        var result = await Sut.TestValidateAsync(CreateValidationContext(model));

        // Assert
        result
            .ShouldHaveValidationErrorFor(x => x.DataType)
            .WithErrorMessage($"Question {questionId}: '{ModuleColumns.DataType}' column must contain a value");
    }

    [Theory]
    [InlineData("IQA001")]
    [InlineData("IQG001")]
    public async Task ShouldAcceptValidQuestionDtoForIQAOrIQGQuestionsWithAllRequiredFields(string questionId)
    {
        // Arrange
        var model = new QuestionDto
        {
            QuestionId = questionId,
            Category = "Test Category",
            QuestionText = "Test Question",
            QuestionType = "Test Type",
            SectionId = "Test Section",
            Sequence = 1,
            Heading = "Test Heading",
            DataType = "Test Data Type"
        };

        // Act
        var result = await Sut.TestValidateAsync(CreateValidationContext(model));

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task ShouldRejectQuestionDtoWithDuplicateQuestionId()
    {
        // Arrange
        var duplicateQuestionId = "IQT001";
        var questionDtos = new List<QuestionDto>
        {
            new QuestionDto { QuestionId = duplicateQuestionId, Category = "Category1", QuestionText = "Question1", QuestionType = "Type1" },
            new QuestionDto { QuestionId = duplicateQuestionId, Category = "Category2", QuestionText = "Question2", QuestionType = "Type2" }
        };

        var model = questionDtos[1];
        var context = new ValidationContext<QuestionDto>(model);
        context.RootContextData["questionDtos"] = questionDtos;

        // Act
        var result = await Sut.TestValidateAsync(context);

        // Assert
        result
            .ShouldHaveValidationErrorFor(x => x.QuestionId)
            .WithErrorMessage($"Duplicate Question ID detected: {duplicateQuestionId}");
    }
}