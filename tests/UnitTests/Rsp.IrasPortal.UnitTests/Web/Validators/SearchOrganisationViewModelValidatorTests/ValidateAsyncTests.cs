using FluentValidation.TestHelper;
using Rsp.Portal.Web.Models;
using Rsp.Portal.Web.Validators;

namespace Rsp.Portal.UnitTests.Web.Validators.SearchOrganisationViewModelValidatorTests;

public class ValidateAsyncTests : TestServiceBase<SearchOrganisationViewModelValidator>
{
    public class SearchOrganisationViewModelValidatorTests : TestServiceBase<SearchOrganisationViewModelValidator>
    {
        [Fact]
        public async Task ShouldPass_WhenSearchNameTermIsNull()
        {
            var model = new SearchOrganisationViewModel
            {
                Search = new OrganisationSearchModel
                {
                    SearchNameTerm = null
                }
            };

            var result = await Sut.TestValidateAsync(model);

            result.ShouldNotHaveValidationErrorFor(x => x.Search.SearchNameTerm);
        }

        [Fact]
        public async Task ShouldPass_WhenSearchNameTermIsEmpty()
        {
            var model = new SearchOrganisationViewModel
            {
                Search = new OrganisationSearchModel
                {
                    SearchNameTerm = ""
                }
            };

            var result = await Sut.TestValidateAsync(model);

            result.ShouldNotHaveValidationErrorFor(x => x.Search.SearchNameTerm);
        }

        [Fact]
        public async Task ShouldReturnError_WhenSearchNameTermIsTooShort()
        {
            var model = new SearchOrganisationViewModel
            {
                Search = new OrganisationSearchModel
                {
                    SearchNameTerm = "ab"
                }
            };

            var result = await Sut.TestValidateAsync(model);

            result.ShouldHaveValidationErrorFor(x => x.Search.SearchNameTerm)
                  .WithErrorMessage("Provide 3 or more characters to search");
        }

        [Fact]
        public async Task ShouldPass_WhenSearchNameTermIsExactlyThreeCharacters()
        {
            var model = new SearchOrganisationViewModel
            {
                Search = new OrganisationSearchModel
                {
                    SearchNameTerm = "abc"
                }
            };

            var result = await Sut.TestValidateAsync(model);

            result.ShouldNotHaveValidationErrorFor(x => x.Search.SearchNameTerm);
        }

        [Fact]
        public async Task ShouldPass_WhenSearchNameTermIsLongerThanThreeCharacters()
        {
            var model = new SearchOrganisationViewModel
            {
                Search = new OrganisationSearchModel
                {
                    SearchNameTerm = "example"
                }
            };

            var result = await Sut.TestValidateAsync(model);

            result.ShouldNotHaveValidationErrorFor(x => x.Search.SearchNameTerm);
        }
    }
}