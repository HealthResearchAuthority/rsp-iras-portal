using FluentValidation.TestHelper;
using Rsp.Portal.Application.DTOs.Responses;
using Rsp.Portal.Web.Validators;

namespace Rsp.Portal.UnitTests.Web.Validators.RfiResponses;

public class ValidateAsyncTests : TestServiceBase<RfiResponsesDtoValidator>
{
    // ---------- INITIAL RESPONSE ----------

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void InitialResponse_Required_Error_When_Empty_Or_Whitespace(string value)
    {
        var model = new RfiResponsesDTO
        {
            InitialResponse = new() { value }
        };

        var result = Sut.TestValidate(model);

        result.ShouldHaveValidationErrorFor("InitialResponse_0")
            .WithErrorMessage(
                "You have not provided a response to reason. Enter the response to request for further information before you continue.");
    }

    [Fact]
    public void InitialResponse_Over_Max_Length_Is_Invalid()
    {
        var model = new RfiResponsesDTO
        {
            InitialResponse = new() { new string('x', 301) }
        };

        var result = Sut.TestValidate(model);

        result.ShouldHaveValidationErrorFor("InitialResponse_0")
            .WithErrorMessage("The response must be between 1 and 300 characters");
    }

    [Fact]
    public void InitialResponse_Valid_Is_Ok()
    {
        var model = new RfiResponsesDTO
        {
            InitialResponse = new() { "Valid response" }
        };

        var result = Sut.TestValidate(model);

        result.ShouldNotHaveAnyValidationErrors();
    }

    // ---------- REVISE AND AUTHORISE ----------

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void ReviseAndAuthorise_Required_Error(string value)
    {
        var model = new RfiResponsesDTO
        {
            ReviseAndAuthorise = new() { value }
        };

        var result = Sut.TestValidate(model);

        result.ShouldHaveValidationErrorFor("ReviseAndAuthorise_0")
            .WithErrorMessage(
                "You have not revised response to reason. Enter the revision to response before you continue.");
    }

    [Fact]
    public void ReviseAndAuthorise_Over_Max_Length_Is_Invalid()
    {
        var model = new RfiResponsesDTO
        {
            ReviseAndAuthorise = new() { new string('x', 301) }
        };

        var result = Sut.TestValidate(model);

        result.ShouldHaveValidationErrorFor("ReviseAndAuthorise_0")
            .WithErrorMessage("The response must be between 1 and 300 characters");
    }

    [Fact]
    public void ReviseAndAuthorise_Valid_Is_Ok()
    {
        var model = new RfiResponsesDTO
        {
            ReviseAndAuthorise = new() { "Revised text" }
        };

        var result = Sut.TestValidate(model);

        result.ShouldNotHaveAnyValidationErrors();
    }

    // ---------- REASON FOR REVISE AND AUTHORISE ----------

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ReasonForReviseAndAuthorise_Required_Error(string value)
    {
        var model = new RfiResponsesDTO
        {
            ReasonForReviseAndAuthorise = new() { value }
        };

        var result = Sut.TestValidate(model);

        result.ShouldHaveValidationErrorFor("ReasonForReviseAndAuthorise_0")
            .WithErrorMessage(
                "You have not provided a reason. Enter the reason for revised response to reason before you continue.");
    }

    [Fact]
    public void ReasonForReviseAndAuthorise_Over_Max_Length_Is_Invalid()
    {
        var model = new RfiResponsesDTO
        {
            ReasonForReviseAndAuthorise = new() { new string('x', 301) }
        };

        var result = Sut.TestValidate(model);

        result.ShouldHaveValidationErrorFor("ReasonForReviseAndAuthorise_0")
            .WithErrorMessage("The response must be between 1 and 300 characters");
    }

    [Fact]
    public void ReasonForReviseAndAuthorise_Valid_Is_Ok()
    {
        var model = new RfiResponsesDTO
        {
            ReasonForReviseAndAuthorise = new() { "Valid reason" }
        };

        var result = Sut.TestValidate(model);

        result.ShouldNotHaveAnyValidationErrors();
    }

    // ---------- WHEN coverage (Count == 0) ----------

    [Fact]
    public void Empty_Collections_Do_Not_Trigger_Validation()
    {
        var model = new RfiResponsesDTO
        {
            InitialResponse = new(),
            ReviseAndAuthorise = new(),
            ReasonForReviseAndAuthorise = new()
        };

        var result = Sut.TestValidate(model);

        result.ShouldNotHaveAnyValidationErrors();
    }
}