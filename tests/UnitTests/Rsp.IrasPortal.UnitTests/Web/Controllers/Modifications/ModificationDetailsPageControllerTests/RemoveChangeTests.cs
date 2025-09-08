using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Rsp.IrasPortal.Web.Controllers;

namespace Rsp.IrasPortal.UnitTests.Web.Controllers.Modifications.ModificationDetailsPageControllerTests;

public class RemoveChangeTests : TestServiceBase<ProjectModificationController>
{
    [Fact]
    public void RemoveChange_WithSuccessResponse_RedirectsToModificationDetailsPage()
    {
        // Arrange
        var changeId = Guid.NewGuid().ToString();

        // Act
        var result = Sut.RemoveChange(changeId);

        // Assert
        var redirectResult = result.ShouldBeOfType<RedirectToActionResult>();
        redirectResult.ActionName.ShouldBe(nameof(Sut.ModificationDetailsPage));
    }
}