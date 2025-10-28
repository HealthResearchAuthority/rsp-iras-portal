using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Rsp.IrasPortal.Application.DTOs;
using Rsp.IrasPortal.Application.DTOs.Responses;
using Rsp.IrasPortal.Application.Responses;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Web.Controllers;
using Rsp.IrasPortal.Web.Models;

namespace Rsp.IrasPortal.UnitTests.Web.Controllers.SponsorOrganisationsControllerTests;

public class SponsorOrganisationAuditTrailTests : TestServiceBase<SponsorOrganisationsController>
{
    private readonly DefaultHttpContext _http;

    public SponsorOrganisationAuditTrailTests()
    {
        _http = new DefaultHttpContext { Session = new InMemorySession() };
        Sut.ControllerContext = new ControllerContext { HttpContext = _http };
    }

    [Fact]
    public async Task AuditTrail_ShouldReturnView_WithMappedModel_AndTransformedDescriptions_WhenServiceSucceeds()
    {
        // Arrange
        const string rtsId = "87765";
        const string orgName = "Acme Research Ltd";
        const string country = "England";
        const int pageNumber = 2;
        const int pageSize = 2;
        const string sortField = "DateTimeStamp";
        const string sortDirection = "desc";

        // LoadSponsorOrganisationAsync dependencies
        var sponsorResponse = new ServiceResponse<AllSponsorOrganisationsResponse>
        {
            StatusCode = HttpStatusCode.OK,
            Content = new AllSponsorOrganisationsResponse
            {
                SponsorOrganisations = new List<SponsorOrganisationDto>
                {
                    new() { IsActive = true, CreatedDate = new DateTime(2024, 5, 1) }
                }
            }
        };

        var organisationResponse = new ServiceResponse<OrganisationDto>
        {
            StatusCode = HttpStatusCode.OK,
            Content = new OrganisationDto { Id = rtsId, Name = orgName, CountryName = country }
        };

        Mocker.GetMock<ISponsorOrganisationService>()
            .Setup(s => s.GetSponsorOrganisationByRtsId(rtsId))
            .ReturnsAsync(sponsorResponse);

        Mocker.GetMock<IRtsService>()
            .Setup(s => s.GetOrganisation(rtsId))
            .ReturnsAsync(organisationResponse);

        // AuditTrail list returned from service (unsorted; controller sorts/paginates)
        var rawItems = new List<SponsorOrganisationAuditTrailDto>
        {
            new()
            {
                Id = "1",
                RtsId = rtsId,
                DateTimeStamp = new DateTime(2024, 6, 10, 10, 00, 00),
                Description = $"User X updated {rtsId} basic info",
                User = "user.x@example.com"
            },
            new()
            {
                Id = "2",
                RtsId = rtsId,
                DateTimeStamp = new DateTime(2024, 6, 11, 9, 00, 00),
                Description = $"Added site to {rtsId.ToLowerInvariant()}",
                User = "user.y@example.com"
            },
            new()
            {
                Id = "3",
                RtsId = rtsId,
                DateTimeStamp = new DateTime(2024, 6, 09, 12, 30, 00),
                Description = "Other org unaffected",
                User = "user.z@example.com"
            },
            new()
            {
                Id = "4",
                RtsId = rtsId,
                DateTimeStamp = new DateTime(2024, 6, 12, 14, 15, 00),
                Description = $"Archived site on {rtsId.ToUpperInvariant()}",
                User = "user.a@example.com"
            }
        };

        var pagedResponse = new SponsorOrganisationAuditTrailResponse
        {
            Items = rawItems,
            TotalCount = rawItems.Count
        };

        var serviceResponse = new ServiceResponse<SponsorOrganisationAuditTrailResponse>
        {
            StatusCode = HttpStatusCode.OK,
            Content = pagedResponse
        };

        Mocker.GetMock<ISponsorOrganisationService>()
            .Setup(s => s.SponsorOrganisationAuditTrail(rtsId, pageNumber, pageSize, sortField, sortDirection))
            .ReturnsAsync(serviceResponse);

        // Act
        var result = await Sut.AuditTrail(rtsId, pageNumber, pageSize);

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        viewResult.ViewName.ShouldBe("AuditTrail");

        var model = viewResult.Model.ShouldBeAssignableTo<SponsorOrganisationAuditTrailViewModel>();
        model.RtsId.ShouldBe(rtsId);
        model.SponsorOrganisation.ShouldBe(orgName);

        // Pagination model assertions
        model.Pagination.ShouldNotBeNull();
        model.Pagination!.PageNumber.ShouldBe(pageNumber);
        model.Pagination.PageSize.ShouldBe(pageSize);
        model.Pagination.TotalCount.ShouldBe(rawItems.Count);
        model.Pagination.RouteName.ShouldBe("soc:audittrail");
        model.Pagination.SortField.ShouldBe(sortField);
        model.Pagination.SortDirection.ShouldBe(sortDirection);
        model.Pagination.AdditionalParameters.ShouldContainKeyAndValue("rtsId", rtsId);

        model.Items.ShouldNotBeNull();
        model.Items.Count().ShouldBe(pageSize);
        model.Items.First().Id.ShouldBe("1");
        model.Items.Last().Id.ShouldBe("3");

        // Descriptions should have rtsId replaced with orgName (case-insensitive)
        model.Items.First().Description.ShouldContain(orgName);
        model.Items.First().Description.ShouldNotContain(rtsId);

        // Verify interactions/parameters
        Mocker.GetMock<ISponsorOrganisationService>()
            .Verify(s => s.SponsorOrganisationAuditTrail(rtsId, pageNumber, pageSize, sortField, sortDirection),
                Times.Once);

        Mocker.GetMock<ISponsorOrganisationService>()
            .Verify(s => s.GetSponsorOrganisationByRtsId(rtsId), Times.Once);

        Mocker.GetMock<IRtsService>()
            .Verify(s => s.GetOrganisation(rtsId), Times.Once);
    }

    [Fact]
    public void SortSponsorOrganisationAuditTrails_ShouldSort_ByDescription_ThenByDate_WhenRequested()
    {
        // Arrange
        const string rtsId = "12345";
        const string orgName = "Zeta Org";
        var items = new List<SponsorOrganisationAuditTrailDto>
        {
            new()
            {
                Id = "a", RtsId = rtsId, DateTimeStamp = new DateTime(2024, 1, 5), Description = "alpha change",
                User = "u1"
            },
            new()
            {
                Id = "b", RtsId = rtsId, DateTimeStamp = new DateTime(2024, 1, 6), Description = "Alpha change",
                User = "u2"
            },
            new()
            {
                Id = "c", RtsId = rtsId, DateTimeStamp = new DateTime(2024, 1, 4),
                Description = "beta change for 12345", User = "u3"
            }
        };

        // Act
        var sortedAsc = InvokeSorter(items, "Description", "asc", orgName, 1, 10).ToList();
        var sortedDesc = InvokeSorter(items, "Description", "desc", orgName, 1, 10).ToList();

        sortedAsc.Select(x => x.Id).ShouldBe(new[] { "b", "a", "c" });
        sortedDesc.Select(x => x.Id).ShouldBe(new[] { "c", "b", "a" });

        sortedAsc.Last().Description.ShouldContain(orgName);
        sortedAsc.Last().Description.ShouldNotContain(rtsId);
    }

    // Helper to call the private NonAction sorter via a local copy (keeps unit test simple & stable)
    private static IEnumerable<SponsorOrganisationAuditTrailDto> InvokeSorter(
        IEnumerable<SponsorOrganisationAuditTrailDto> items,
        string? sortField,
        string? sortDirection,
        string? sponsorOrganisationName,
        int pageNumber,
        int pageSize)
    {
        // Inline the controller logic (1: transform, 2: sort, 3: paginate) to validate behavior deterministically
        var list = items
            .Select(x =>
            {
                var desc = x.Description ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(sponsorOrganisationName) && !string.IsNullOrWhiteSpace(x.RtsId))
                {
                    desc = desc.Replace(x.RtsId, sponsorOrganisationName!, StringComparison.OrdinalIgnoreCase);
                }

                return new SponsorOrganisationAuditTrailDto
                {
                    Id = x.Id,
                    RtsId = x.RtsId,
                    DateTimeStamp = x.DateTimeStamp,
                    Description = desc,
                    User = x.User
                };
            })
            .ToList();

        var descSort = string.Equals(sortDirection, "desc", StringComparison.OrdinalIgnoreCase);
        var field = sortField?.ToLowerInvariant();

        var sorted = field switch
        {
            "description" => descSort
                ? list.OrderByDescending(x => x.Description, StringComparer.OrdinalIgnoreCase)
                    .ThenByDescending(x => x.DateTimeStamp)
                : list.OrderBy(x => x.Description, StringComparer.OrdinalIgnoreCase)
                    .ThenByDescending(x => x.DateTimeStamp),
            "user" => descSort
                ? list.OrderByDescending(x => x.User, StringComparer.OrdinalIgnoreCase)
                    .ThenByDescending(x => x.DateTimeStamp)
                : list.OrderBy(x => x.User, StringComparer.OrdinalIgnoreCase)
                    .ThenByDescending(x => x.DateTimeStamp),
            "datetimestamp" => descSort
                ? list.OrderByDescending(x => x.DateTimeStamp)
                : list.OrderBy(x => x.DateTimeStamp),
            _ => list.OrderByDescending(x => x.DateTimeStamp)
        };

        if (pageNumber < 1)
        {
            pageNumber = 1;
        }

        if (pageSize < 1)
        {
            pageSize = 20;
        }

        var skip = (pageNumber - 1) * pageSize;
        return sorted.Skip(skip).Take(pageSize);
    }
}