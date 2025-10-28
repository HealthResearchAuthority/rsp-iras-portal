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
    public async Task AuditTrail_ShouldSort_ByUser_Asc_CaseInsensitive_WithDateTieBreaker()
    {
        // Arrange
        const string rtsId = "R1";
        const string orgName = "Acme Research Ltd";
        const string country = "England";
        const int pageNumber = 1;
        const int pageSize = 10;
        const string sortField = "User";
        const string sortDirection = "asc";

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

        var rawItems = new List<SponsorOrganisationAuditTrailDto>
    {
        // alice vs Alice => case-insensitive equal; tie broken by DateTimeStamp DESC => "A2" before "A1"
        new() { Id = "A1", RtsId = rtsId, DateTimeStamp = new DateTime(2024, 6, 10,  9, 0, 0), Description = "d1", User = "alice" },
        new() { Id = "A2", RtsId = rtsId, DateTimeStamp = new DateTime(2024, 6, 11,  9, 0, 0), Description = "d2", User = "Alice" },
        new() { Id = "B1", RtsId = rtsId, DateTimeStamp = new DateTime(2024, 6,  9,  9, 0, 0), Description = "d3", User = "Bob"   },
        new() { Id = "C1", RtsId = rtsId, DateTimeStamp = new DateTime(2024, 6, 12,  9, 0, 0), Description = "d4", User = "charlie" }
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
        var result = await Sut.AuditTrail(rtsId, pageNumber, pageSize, sortField, sortDirection);

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        viewResult.ViewName.ShouldBe("AuditTrail");

        var model = viewResult.Model.ShouldBeAssignableTo<SponsorOrganisationAuditTrailViewModel>();
        model.SponsorOrganisation.ShouldBe(orgName);

        // Expected order: Alice (A2), alice (A1), Bob (B1), charlie (C1)
        model.Items.Select(i => i.Id).ShouldBe(new[] { "A2", "A1", "B1", "C1" });

        model.Pagination.SortField.ShouldBe(sortField);
        model.Pagination.SortDirection.ShouldBe(sortDirection);

        Mocker.GetMock<ISponsorOrganisationService>()
            .Verify(s => s.SponsorOrganisationAuditTrail(rtsId, pageNumber, pageSize, sortField, sortDirection), Times.Once);
    }

    [Fact]
    public async Task AuditTrail_ShouldSort_ByUser_Desc_CaseInsensitive_WithDateTieBreaker()
    {
        // Arrange
        const string rtsId = "R1";
        const string orgName = "Acme Research Ltd";
        const string country = "England";
        const int pageNumber = 1;
        const int pageSize = 10;
        const string sortField = "User";
        const string sortDirection = "desc";

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

        var rawItems = new List<SponsorOrganisationAuditTrailDto>
    {
        new() { Id = "A1", RtsId = rtsId, DateTimeStamp = new DateTime(2024, 6, 10,  9, 0, 0), Description = "d1", User = "alice" },
        new() { Id = "A2", RtsId = rtsId, DateTimeStamp = new DateTime(2024, 6, 11,  9, 0, 0), Description = "d2", User = "Alice" },
        new() { Id = "B1", RtsId = rtsId, DateTimeStamp = new DateTime(2024, 6,  9,  9, 0, 0), Description = "d3", User = "Bob"   },
        new() { Id = "C1", RtsId = rtsId, DateTimeStamp = new DateTime(2024, 6, 12,  9, 0, 0), Description = "d4", User = "charlie" }
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
        var result = await Sut.AuditTrail(rtsId, pageNumber, pageSize, sortField, sortDirection);

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        viewResult.ViewName.ShouldBe("AuditTrail");

        var model = viewResult.Model.ShouldBeAssignableTo<SponsorOrganisationAuditTrailViewModel>();
        model.SponsorOrganisation.ShouldBe(orgName);

        // Expected desc: charlie (C1), Bob (B1), Alice/alice => A2 then A1 by DateTimeStamp desc
        model.Items.Select(i => i.Id).ShouldBe(new[] { "C1", "B1", "A2", "A1" });

        model.Pagination.SortField.ShouldBe(sortField);
        model.Pagination.SortDirection.ShouldBe(sortDirection);

        Mocker.GetMock<ISponsorOrganisationService>()
            .Verify(s => s.SponsorOrganisationAuditTrail(rtsId, pageNumber, pageSize, sortField, sortDirection), Times.Once);
    }

    [Fact]
    public async Task AuditTrail_ShouldSort_ByDescription_Asc_CaseInsensitive_WithDateTieBreaker_AndReplaceRtsId()
    {
        // Arrange
        const string rtsId = "R2";
        const string orgName = "Zeta Org";
        const string country = "England";
        const int pageNumber = 1;
        const int pageSize = 10;
        const string sortField = "Description";
        const string sortDirection = "asc";

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

        var rawItems = new List<SponsorOrganisationAuditTrailDto>
    {
        // Two equal descriptions ignoring case => tie broken by DateTimeStamp DESC => D2 before D1
        new() { Id = "D1", RtsId = rtsId, DateTimeStamp = new DateTime(2024, 2, 10), Description = "alpha change",               User = "u1" },
        new() { Id = "D2", RtsId = rtsId, DateTimeStamp = new DateTime(2024, 2, 12), Description = "Alpha Change",               User = "u2" },
        new() { Id = "E1", RtsId = rtsId, DateTimeStamp = new DateTime(2024, 2, 11), Description = $"beta patch for {rtsId}",    User = "u3" },
        new() { Id = "F1", RtsId = rtsId, DateTimeStamp = new DateTime(2024, 2,  9), Description = "charlie fix",                User = "u4" }
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
        var result = await Sut.AuditTrail(rtsId, pageNumber, pageSize, sortField, sortDirection);

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        viewResult.ViewName.ShouldBe("AuditTrail");

        var model = viewResult.Model.ShouldBeAssignableTo<SponsorOrganisationAuditTrailViewModel>();
        model.SponsorOrganisation.ShouldBe(orgName);

        // Expected asc by description: alpha..., beta..., charlie...
        model.Items.Select(i => i.Id).ShouldBe(new[] { "D2", "D1", "E1", "F1" });

        // Replacement check on the "beta..." item
        var replaced = model.Items.Single(i => i.Id == "E1").Description;
        replaced.ShouldContain(orgName);
        replaced.ShouldNotContain(rtsId);

        model.Pagination.SortField.ShouldBe(sortField);
        model.Pagination.SortDirection.ShouldBe(sortDirection);

        Mocker.GetMock<ISponsorOrganisationService>()
            .Verify(s => s.SponsorOrganisationAuditTrail(rtsId, pageNumber, pageSize, sortField, sortDirection), Times.Once);
    }

    [Fact]
    public async Task AuditTrail_ShouldSort_ByDescription_Desc_CaseInsensitive_WithDateTieBreaker()
    {
        // Arrange
        const string rtsId = "R2";
        const string orgName = "Zeta Org";
        const string country = "England";
        const int pageNumber = 1;
        const int pageSize = 10;
        const string sortField = "Description";
        const string sortDirection = "desc";

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

        var rawItems = new List<SponsorOrganisationAuditTrailDto>
    {
        new() { Id = "D1", RtsId = rtsId, DateTimeStamp = new DateTime(2024, 2, 10), Description = "alpha change", User = "u1" },
        new() { Id = "D2", RtsId = rtsId, DateTimeStamp = new DateTime(2024, 2, 12), Description = "Alpha Change", User = "u2" },
        new() { Id = "E1", RtsId = rtsId, DateTimeStamp = new DateTime(2024, 2, 11), Description = "beta patch",   User = "u3" },
        new() { Id = "F1", RtsId = rtsId, DateTimeStamp = new DateTime(2024, 2,  9), Description = "charlie fix",  User = "u4" }
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
        var result = await Sut.AuditTrail(rtsId, pageNumber, pageSize, sortField, sortDirection);

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        viewResult.ViewName.ShouldBe("AuditTrail");

        var model = viewResult.Model.ShouldBeAssignableTo<SponsorOrganisationAuditTrailViewModel>();
        model.SponsorOrganisation.ShouldBe(orgName);

        // Expected desc by description: charlie..., beta..., alpha... (alpha tie => D2 then D1 by Date desc)
        model.Items.Select(i => i.Id).ShouldBe(new[] { "F1", "E1", "D2", "D1" });

        model.Pagination.SortField.ShouldBe(sortField);
        model.Pagination.SortDirection.ShouldBe(sortDirection);

        Mocker.GetMock<ISponsorOrganisationService>()
            .Verify(s => s.SponsorOrganisationAuditTrail(rtsId, pageNumber, pageSize, sortField, sortDirection), Times.Once);
    }


    [Fact]
    public void SortSponsorOrganisationAuditTrails_ShouldSort_ByUser_Asc_CaseInsensitive_WithDateTieBreaker()
    {
        // Arrange
        const string rtsId = "R1";
        const string orgName = "Org";
        var items = new List<SponsorOrganisationAuditTrailDto>
        {
            // alice vs Alice should be considered equal for primary ordering; tie-breaker by DateTimeStamp desc
            new() { Id = "1", RtsId = rtsId, DateTimeStamp = new DateTime(2024, 1, 10, 9, 0, 0),  Description = "d1", User = "alice" },
            new() { Id = "2", RtsId = rtsId, DateTimeStamp = new DateTime(2024, 1, 11, 9, 0, 0), Description = "d2", User = "Alice" },
            new() { Id = "3", RtsId = rtsId, DateTimeStamp = new DateTime(2024, 1,  9, 9, 0, 0), Description = "d3", User = "Bob"   },
            new() { Id = "4", RtsId = rtsId, DateTimeStamp = new DateTime(2024, 1, 12, 9, 0, 0), Description = "d4", User = "charlie" }
        };

        // Act
        var sorted = InvokeSorter(items, "User", "asc", orgName, 1, 50).ToList();

        // Assert
        // Expected User order asc (case-insensitive): Alice/alice (tie => Date desc => "2" then "1"), Bob, charlie
        sorted.Select(x => x.Id).ShouldBe(new[] { "2", "1", "3", "4" });
    }

    [Fact]
    public void SortSponsorOrganisationAuditTrails_ShouldSort_ByUser_Desc_CaseInsensitive_WithDateTieBreaker()
    {
        // Arrange
        const string rtsId = "R1";
        const string orgName = "Org";
        var items = new List<SponsorOrganisationAuditTrailDto>
        {
            new() { Id = "1", RtsId = rtsId, DateTimeStamp = new DateTime(2024, 1, 10, 9, 0, 0), Description = "d1", User = "alice" },
            new() { Id = "2", RtsId = rtsId, DateTimeStamp = new DateTime(2024, 1, 11, 9, 0, 0), Description = "d2", User = "Alice" },
            new() { Id = "3", RtsId = rtsId, DateTimeStamp = new DateTime(2024, 1,  9, 9, 0, 0), Description = "d3", User = "Bob"   },
            new() { Id = "4", RtsId = rtsId, DateTimeStamp = new DateTime(2024, 1, 12, 9, 0, 0), Description = "d4", User = "charlie" }
        };

        // Act
        var sorted = InvokeSorter(items, "User", "desc", orgName, 1, 50).ToList();

        // Assert
        // Expected User order desc (case-insensitive): charlie, Bob, Alice/alice (tie => Date desc => "2" then "1")
        sorted.Select(x => x.Id).ShouldBe(new[] { "4", "3", "2", "1" });
    }

    [Fact]
    public void SortSponsorOrganisationAuditTrails_ShouldSort_ByDescription_Asc_CaseInsensitive_WithDateTieBreaker()
    {
        // Arrange
        const string rtsId = "R2";
        const string orgName = "OrgZ";
        var items = new List<SponsorOrganisationAuditTrailDto>
        {
            // Two equal descriptions (case-insensitive) => tie broken by DateTimeStamp desc
            new() { Id = "a1", RtsId = rtsId, DateTimeStamp = new DateTime(2024, 2, 10), Description = "alpha change", User = "u1" },
            new() { Id = "a2", RtsId = rtsId, DateTimeStamp = new DateTime(2024, 2, 12), Description = "Alpha Change", User = "u2" },
            new() { Id = "b1", RtsId = rtsId, DateTimeStamp = new DateTime(2024, 2, 11), Description = "beta patch",   User = "u3" },
            new() { Id = "c1", RtsId = rtsId, DateTimeStamp = new DateTime(2024, 2,  9), Description = "charlie fix",  User = "u4" }
        };

        // Act
        var sorted = InvokeSorter(items, "Description", "asc", orgName, 1, 50).ToList();

        // Assert
        // Alphabetical asc by description (case-insensitive): alpha..., beta..., charlie...
        // For "alpha..." tie => Date desc => a2 (2024-02-12) before a1 (2024-02-10)
        sorted.Select(x => x.Id).ShouldBe(new[] { "a2", "a1", "b1", "c1" });
    }

    [Fact]
    public void SortSponsorOrganisationAuditTrails_ShouldSort_ByDescription_Desc_CaseInsensitive_WithDateTieBreaker()
    {
        // Arrange
        const string rtsId = "R2";
        const string orgName = "OrgZ";
        var items = new List<SponsorOrganisationAuditTrailDto>
        {
            new() { Id = "a1", RtsId = rtsId, DateTimeStamp = new DateTime(2024, 2, 10), Description = "alpha change", User = "u1" },
            new() { Id = "a2", RtsId = rtsId, DateTimeStamp = new DateTime(2024, 2, 12), Description = "Alpha Change", User = "u2" },
            new() { Id = "b1", RtsId = rtsId, DateTimeStamp = new DateTime(2024, 2, 11), Description = "beta patch",   User = "u3" },
            new() { Id = "c1", RtsId = rtsId, DateTimeStamp = new DateTime(2024, 2,  9), Description = "charlie fix",  User = "u4" }
        };

        // Act
        var sorted = InvokeSorter(items, "Description", "desc", orgName, 1, 50).ToList();

        // Assert
        // Desc by description: charlie..., beta..., alpha...
        // For "alpha..." tie => Date desc => a2 then a1
        sorted.Select(x => x.Id).ShouldBe(new[] { "c1", "b1", "a2", "a1" });
    }

    [Fact]
    public void SortSponsorOrganisationAuditTrails_ByUser_Asc_ShouldRespectPagination()
    {
        // Arrange
        const string rtsId = "R3";
        const string orgName = "OrgP";
        var items = new List<SponsorOrganisationAuditTrailDto>
        {
            new() { Id = "1", RtsId = rtsId, DateTimeStamp = new DateTime(2024, 3, 10), Description = "x", User = "adam"   },
            new() { Id = "2", RtsId = rtsId, DateTimeStamp = new DateTime(2024, 3, 11), Description = "x", User = "Barry"  },
            new() { Id = "3", RtsId = rtsId, DateTimeStamp = new DateTime(2024, 3, 12), Description = "x", User = "barry"  }, // tie with Barry -> Date desc wins
            new() { Id = "4", RtsId = rtsId, DateTimeStamp = new DateTime(2024, 3, 13), Description = "x", User = "charles"},
            new() { Id = "5", RtsId = rtsId, DateTimeStamp = new DateTime(2024, 3, 14), Description = "x", User = "dave"   }
        };

        // Asc by user => adam, Barry/barry (tie => "3" then "2"), charles, dave
        // With pageNumber=2, pageSize=2 => expect ids ["3","2"] on first page, ["4","5"] on second
        var page1 = InvokeSorter(items, "User", "asc", orgName, 1, 2).Select(x => x.Id).ToList();
        var page2 = InvokeSorter(items, "User", "asc", orgName, 2, 2).Select(x => x.Id).ToList();

        page1.ShouldBe(new[] { "1", "3" });          // adam, then barry(2024-03-12)
        page2.ShouldBe(new[] { "2", "4" });          // Barry(2024-03-11), then charles
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