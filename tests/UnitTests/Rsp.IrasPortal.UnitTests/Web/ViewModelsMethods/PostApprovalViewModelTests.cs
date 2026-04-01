using Rsp.IrasPortal.Application.Enum;
using Rsp.Portal.Application.Constants;
using Rsp.Portal.Application.DTOs.Responses;
using Rsp.Portal.Web.Models;

namespace Rsp.IrasPortal.UnitTests.Web.ViewModelsMethods;

public class PostApprovalViewModelTests
{
    [Fact]
    public void CanCreateNewModification_When_Modification_InTransactionStatus_Returns_InvalidStatus()
    {
        // Arrange
        var model = new PostApprovalViewModel
        {
            Modifications = new List<PostApprovalModificationsModel>
            {
                new() { Status = ModificationStatus.InDraft }
            }
        };

        // Act
        var result = model.CanCreateNewModification();

        // Assert
        result.ShouldBe(ModificationCreationCheckResult.InvalidStatus);
    }

    [Fact]
    public void CanCreateNewModification_When_Change_WithReviewBody_And_BlockedArea_Returns_BlockedSpecificAreaOfChange()
    {
        // Arrange
        var model = new PostApprovalViewModel
        {
            Modifications = new List<PostApprovalModificationsModel>(),
            AllProjectModificationChanges = new List<ProjectModificationChangeResponse>
            {
                new()
                {
                    Status = ModificationStatus.WithReviewBody,
                    SpecificAreaOfChange = AreasOfChange.ProjectHalt
                }
            }
        };

        // Act
        var result = model.CanCreateNewModification();

        // Assert
        result.ShouldBe(ModificationCreationCheckResult.BlockedSpecificAreaOfChange);
    }

    [Fact]
    public void CanCreateNewModification_When_Change_Has_BlockedArea_But_Status_Not_WithReviewBody_Returns_Success()
    {
        // Arrange
        var model = new PostApprovalViewModel
        {
            Modifications = new List<PostApprovalModificationsModel>(),
            AllProjectModificationChanges = new List<ProjectModificationChangeResponse>
            {
                new()
                {
                    Status = ModificationStatus.Approved,
                    SpecificAreaOfChange = AreasOfChange.ProjectHalt
                }
            }
        };

        // Act
        var result = model.CanCreateNewModification();

        // Assert
        result.ShouldBe(ModificationCreationCheckResult.Success);
    }

    [Fact]
    public void CanCreateNewModification_When_No_Blocks_Returns_Success()
    {
        // Arrange
        var model = new PostApprovalViewModel
        {
            Modifications = new List<PostApprovalModificationsModel>(),
            AllProjectModificationChanges = new List<ProjectModificationChangeResponse>()
        };

        // Act
        var result = model.CanCreateNewModification();

        // Assert
        result.ShouldBe(ModificationCreationCheckResult.Success);
    }
}