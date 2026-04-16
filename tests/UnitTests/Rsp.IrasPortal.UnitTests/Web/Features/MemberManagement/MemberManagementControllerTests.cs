using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Rsp.Portal.Web.Features.MemberManagement.Controllers;

namespace Rsp.Portal.UnitTests.Web.Features.MemberManagement;

public class MemberManagementControllerTests : TestServiceBase<MemberManagementController>
{
    private readonly DefaultHttpContext _http;

    public MemberManagementControllerTests()
    {
        _http = new DefaultHttpContext { Session = new InMemorySession() };
        Sut.ControllerContext = new ControllerContext { HttpContext = _http };
    }

    [Fact]
    public async Task MemberManagement_ShouldReturnView()
    {
        // Act
        var result = await Sut.MemberManagement();

        // Assert
        result.ShouldBeOfType<ViewResult>();
    }
}