using FluentValidation.TestHelper;
using Rsp.IrasPortal.Web.Features.SponsorWorkspace.MyOrganisations.Models;
using Rsp.IrasPortal.Web.Validators;

namespace Rsp.IrasPortal.UnitTests.Web.Validators.SponsorOrganisationProjectRecordsSearchValidatorTests;

public class ValidateAsyncTests : TestServiceBase<SponsorOrganisationProjectSearchModelValidator>
{
    [Fact]
    public async Task ShouldReturnErrorIfFromDatePartsInvalid()
    {
        var model = new SponsorOrganisationProjectSearchModel
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
        var model = new SponsorOrganisationProjectSearchModel
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
        var model = new SponsorOrganisationProjectSearchModel
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
            .WithErrorMessage("The date you’ve selected is before the search above");
    }

    [Fact]
    public async Task ShouldPassWhenOnlyValidFromDatePartsProvided()
    {
        var model = new SponsorOrganisationProjectSearchModel
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
        var model = new SponsorOrganisationProjectSearchModel
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
        var model = new SponsorOrganisationProjectSearchModel
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