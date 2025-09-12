using Microsoft.AspNetCore.Mvc;
using Rsp.IrasPortal.Web.Extensions;
using Rsp.IrasPortal.Web.Models;

namespace Rsp.IrasPortal.Web.Controllers;

public partial class ProjectModificationController
{
    [HttpGet]
    public IActionResult ReviewAllChanges()
    {
        var viewModel = TempData.PopulateBaseProjectModificationProperties(new ModificationDetailsPageViewModel());

        viewModel.ModificationId = Guid.NewGuid();
        viewModel.Status = "Draft";
        viewModel.ModificationType = "Minor modification";
        viewModel.Category = "{A > B/C > B > C > New site > N/A}";
        viewModel.ReviewType = "No review required";
        viewModel.ModificationChanges = new List<ModificationChangeModel>
        {
            new ModificationChangeModel
            {
                ModificationChangeId = Guid.NewGuid(),
                ModificationType = "Minor Modification",
                Category = "A > B/C",
                ReviewType = "No review required",
                AreaOfChangeName = "Planned End Date",
                SpecificChangeName = "PlannedEndDateChanged",
                SpecificChangeAnswer = "The planned end date has been moved by 3 months.",
                ChangeStatus = "Change ready for submission",
                SupportingDocuments = new List<SupportingDocumentModel>
                {
                    new SupportingDocumentModel
                    {
                        Name = "GP-Patient-participation-1.5.doc",
                        Link = "https://example.com/docs/GP-Patient-participation-1.5.doc",
                        Date = new DateTime(2025, 9, 1, 0, 0, 0, DateTimeKind.Utc)
                    }
                }
            },
            new ModificationChangeModel
            {
                ModificationChangeId = Guid.NewGuid(),
                ModificationType = "Minor Modification",
                Category = "New site",
                ReviewType = "No review required",
                AreaOfChangeName = "Modification Document",
                SpecificChangeName = "Other Modification Change",
                SpecificChangeAnswer = "Additional documentation has been submitted for review.",
                ChangeStatus = "Change ready for submission",
                SupportingDocuments = new List<SupportingDocumentModel>
                {
                    new SupportingDocumentModel
                    {
                        Name = "GP-Patient-participation-1.5.doc",
                        Link = "https://example.com/docs/GP-Patient-participation-1.5.doc",
                        Date = new DateTime(2025, 9, 1, 0, 0, 0, DateTimeKind.Utc)
                    },
                    new SupportingDocumentModel
                    {
                        Name = "GP-Patient-participation-1.6.doc",
                        Link = "https://example.com/docs/GP-Patient-participation-1.6.doc",
                        Date = new DateTime(2025, 9, 2, 0, 0, 0, DateTimeKind.Utc)
                    }
                }
            }
        };

        viewModel.SponsorReference = new SponsorReferenceViewModel
        {
            SponsorModificationReference = "MOD-2021-045",
            SponsorModificationDate = new DateViewModel
            {
                Day = "10",
                Month = "03",
                Year = "2021"
            },
            MainChangesDescription = "Updated project scope and revised budget allocation."
        };

        return View(viewModel);
    }

    [HttpGet]
    public IActionResult ConfirmRemoveChangeReviewAll(string modificationChangeId, string modificationChangeName)
    {
        ViewData["ReviewAllPage"] = true;
        var viewModel = TempData.PopulateBaseProjectModificationProperties(new ModificationDetailsPageViewModel());
        return View("ConfirmRemoveChange", (viewModel, modificationChangeId, modificationChangeName));
    }
}