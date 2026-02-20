using FluentValidation.TestHelper;
using Rsp.Portal.Web.Models;
using Rsp.Portal.Web.Validators;

namespace Rsp.Portal.UnitTests.Web.Validators.ApprovalsSearchModelValidatorTests;

public class ValidateAsyncTests : TestServiceBase<ApprovalsSearchModelValidator>
{
    [Fact]
    public async Task ShouldReturnErrorIfFromDatePartsInvalid()
    {
        var model = new ApprovalsSearchModel
        {
            FromDay = "31",
            FromMonth = "02",
            FromYear = "2024"
        };

        var result = await Sut.TestValidateAsync(model);

        result.ShouldHaveValidationErrorFor(x => x.FromDate)
            .WithErrorMessage("'Search from' date must be in the correct format");
    }

    [Fact]
    public async Task ShouldReturnErrorIfToDatePartsInvalid()
    {
        var model = new ApprovalsSearchModel
        {
            ToDay = "99",
            ToMonth = "99",
            ToYear = "abcd"
        };

        var result = await Sut.TestValidateAsync(model);

        result.ShouldHaveValidationErrorFor(x => x.ToDate)
            .WithErrorMessage("'Search to' date must be in the correct format");
    }

    [Fact]
    public async Task ShouldReturnErrorIfToDateBeforeFromDate()
    {
        var model = new ApprovalsSearchModel
        {
            FromDay = "15",
            FromMonth = "06",
            FromYear = "2024",
            ToDay = "10",
            ToMonth = "06",
            ToYear = "2024"
        };

        var result = await Sut.TestValidateAsync(model);

        result.ShouldHaveValidationErrorFor(x => x.ToDate)
            .WithErrorMessage("'Search from' date must be the same as or before 'Search to' date");
    }

    [Fact]
    public async Task ShouldPassWhenOnlyValidFromDatePartsProvided()
    {
        var model = new ApprovalsSearchModel
        {
            FromDay = "01",
            FromMonth = "06",
            FromYear = "2024"
        };

        var result = await Sut.TestValidateAsync(model);

        result.ShouldNotHaveValidationErrorFor(x => x.FromDate);
    }

    [Fact]
    public async Task ShouldPassWhenOnlyValidToDatePartsProvided()
    {
        var model = new ApprovalsSearchModel
        {
            ToDay = "20",
            ToMonth = "07",
            ToYear = "2024"
        };

        var result = await Sut.TestValidateAsync(model);

        result.ShouldNotHaveValidationErrorFor(x => x.ToDate);
    }

    [Fact]
    public async Task ShouldPassWhenBothDatesAreValidAndChronological()
    {
        var model = new ApprovalsSearchModel
        {
            FromDay = "01",
            FromMonth = "06",
            FromYear = "2024",
            ToDay = "01",
            ToMonth = "07",
            ToYear = "2024"
        };

        var result = await Sut.TestValidateAsync(model);

        result.ShouldNotHaveAnyValidationErrors();
    }
}