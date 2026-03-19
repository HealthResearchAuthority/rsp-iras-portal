using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Rsp.Portal.Application.DTOs.Requests;
using Rsp.Portal.UnitTests;
using Rsp.Portal.Web.Extensions;
using Rsp.Portal.Web.Models;
using Rsp.Portal.Web.Validators.Helpers;

namespace Rsp.IrasPortal.UnitTests.Web.Extensions.ControllerExtensionsTests;

public class ValidateQuestionnaireTests : TestServiceBase<ControllerToExtensionTests>
{
    [Fact]
    public async Task Should_Add_ModelState_Errors_When_Validation_Fails()
    {
        // Arrange
        var controller = Sut;

        var validator = Mocker.GetMock<IValidator<QuestionnaireViewModel>>();

        var model = new QuestionnaireViewModel
        {
            Questions = new List<QuestionViewModel>(),
            ProjectRecordAnswers = new Dictionary<string, RespondentAnswerDto>()
        };

        var failures = new List<ValidationFailure>
        {
            new ValidationFailure("Field1", "Error 1"),
            new ValidationFailure("Field2", "Error 2")
        };

        validator
            .Setup(v => v.ValidateAsync(
                It.IsAny<ValidationContext<QuestionnaireViewModel>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(failures));

        // Act
        var result = await controller.ValidateQuestionnaire(
            validator.Object,
            model,
            validateMandatory: false,
            addModelErrors: true);

        // Assert
        result.ShouldBeFalse();
        controller.ModelState.Count.ShouldBe(2);

        controller.ModelState["Field1"].Errors[0].ErrorMessage.ShouldBe("Error 1");
        controller.ModelState["Field2"].Errors[0].ErrorMessage.ShouldBe("Error 2");
    }

    [Fact]
    public async Task Should_Return_True_When_Validation_Passes()
    {
        // Arrange
        var controller = Sut;
        var validator = Mocker.GetMock<IValidator<QuestionnaireViewModel>>();

        var model = new QuestionnaireViewModel
        {
            Questions = new List<QuestionViewModel>(),
            ProjectRecordAnswers = new Dictionary<string, RespondentAnswerDto>()
        };

        validator
            .Setup(v => v.ValidateAsync(
                It.IsAny<ValidationContext<QuestionnaireViewModel>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        // Act
        var result = await controller.ValidateQuestionnaire(
            validator.Object,
            model);

        // Assert
        result.ShouldBeTrue();
        controller.ModelState.IsValid.ShouldBeTrue();
    }

    [Fact]
    public async Task Should_Set_ValidateMandatory_Flag_In_RootContextData()
    {
        // Arrange
        var controller = Sut;

        var validator = Mocker.GetMock<IValidator<QuestionnaireViewModel>>();

        IValidationContext? passedContext = null;

        validator
            .Setup(v => v.ValidateAsync(
                It.IsAny<IValidationContext>(),
                It.IsAny<CancellationToken>()))
            .Callback<IValidationContext, CancellationToken>((ctx, _) =>
            {
                passedContext = ctx;
            })
            .ReturnsAsync(new ValidationResult(new[]
            {
            new ValidationFailure("Field", "err")
            }));

        var model = new QuestionnaireViewModel
        {
            Questions = new(),
            ProjectRecordAnswers = new()
        };

        // Act
        var result = await controller.ValidateQuestionnaire(
            validator.Object,
            model,
            validateMandatory: true);

        // Assert
        result.ShouldBeFalse();

        passedContext.ShouldNotBeNull();
        passedContext!.RootContextData["ValidateMandatoryOnly"].ShouldBe(true);
        passedContext!.RootContextData["questions"].ShouldBeSameAs(model.Questions);
        passedContext!.RootContextData["ProjectRecordAnswers"].ShouldBeSameAs(model.ProjectRecordAnswers);
    }

    [Fact]
    public async Task Should_Use_Adjusted_PropertyName_When_CustomState_Is_QuestionViewModel()
    {
        // Arrange
        var controller = Sut;

        var validator = Mocker.GetMock<IValidator<QuestionnaireViewModel>>();

        var qvm = new QuestionViewModel { Index = 5 };

        var failure = new ValidationFailure("SomeField", "Msg")
        {
            CustomState = qvm
        };

        validator
            .Setup(v => v.ValidateAsync(
                It.IsAny<ValidationContext<QuestionnaireViewModel>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(new[] { failure }));

        var model = new QuestionnaireViewModel
        {
            Questions = new(),
            ProjectRecordAnswers = new()
        };

        // Act
        var result = await controller.ValidateQuestionnaire(
            validator.Object,
            model);

        // Assert
        result.ShouldBeFalse();

        var expectedName = PropertyNameHelper.AdjustPropertyName("SomeField", 5);

        controller.ModelState.ContainsKey(expectedName).ShouldBeTrue();
    }

    [Fact]
    public async Task Should_Not_Add_Errors_When_addModelErrors_Is_False()
    {
        // Arrange
        var controller = Sut;

        var validator = Mocker.GetMock<IValidator<QuestionnaireViewModel>>();

        var failures = new List<ValidationFailure>
    {
        new("F1", "Err")
    };

        validator
            .Setup(v => v.ValidateAsync(
                It.IsAny<ValidationContext<QuestionnaireViewModel>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(failures));

        var model = new QuestionnaireViewModel
        {
            Questions = new(),
            ProjectRecordAnswers = new()
        };

        // Act
        var result = await controller.ValidateQuestionnaire(
            validator.Object,
            model,
            addModelErrors: false);

        // Assert
        result.ShouldBeFalse();
        controller.ModelState.ShouldBeEmpty();
    }
}

public class ControllerToExtensionTests : Controller
{
}