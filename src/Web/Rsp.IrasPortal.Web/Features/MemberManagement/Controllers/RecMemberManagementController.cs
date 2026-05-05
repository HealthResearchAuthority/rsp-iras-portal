using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.FeatureManagement.Mvc;
using Rsp.IrasPortal.Web.Features.MemberManagement.Models;
using Rsp.IrasPortal.Web.Helpers;
using Rsp.Portal.Application.Constants;
using Rsp.Portal.Application.DTOs;
using Rsp.Portal.Application.DTOs.Responses;
using Rsp.Portal.Application.Filters;
using Rsp.Portal.Application.Services;
using Rsp.Portal.Domain.AccessControl;
using Rsp.Portal.Domain.Identity;
using Rsp.Portal.Web.Areas.Admin.Models;
using Rsp.Portal.Web.Extensions;
using Rsp.Portal.Web.Models;

namespace Rsp.IrasPortal.Web.Features.MemberManagement.Controllers;

[Authorize(Policy = Workspaces.MemberManagement)]
[Authorize(Policy = Permissions.MemberManagement.ResearchEthicsCommittees_ManageMembers)]
[Route("membermanagement/[action]", Name = "mm:[action]")]
[FeatureGate(FeatureFlags.RecMemberManagement)]
public class RecMemberManagementController(
    IReviewBodyService reviewBodyService,
    IUserManagementService userService,
    IValidator<AddRecMemberViewModel> addMemberValidator,
    IValidator<RecMemberViewModel> recMemberValidator
    ) : Controller
{
    [HttpGet]
    public async Task<IActionResult> SearchRecMember(Guid recId)
    {
        // Check if review body exists before showing the page to add a member to the review body
        var reviewBodyResponse = await reviewBodyService.GetReviewBodyById(recId);

        if (!reviewBodyResponse.IsSuccessStatusCode
            || reviewBodyResponse.Content == null)
        {
            // If the review body does not exist, return a 404 not found response
            return this.ServiceError(reviewBodyResponse);
        }

        if (!await MemberManagementHelper.UserHasAccess(reviewBodyResponse.Content, User, userService))
        {
            // if user does not have access to the review body, return 403 forbidden
            return Forbid();
        }

        var viewModel = new AddRecMemberViewModel
        {
            RecId = recId,
            RecName = reviewBodyResponse.Content.RegulatoryBodyName
        };

        return View(viewModel);
    }

    [HttpPost]
    public async Task<IActionResult> SearchRecMember(AddRecMemberViewModel model)
    {
        // do validation
        var validationResult = await addMemberValidator.ValidateAsync(model);

        if (!validationResult.IsValid)
        {
            foreach (var error in validationResult.Errors)
            {
                ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
            }

            return View(model);
        }

        var userResponse = await userService.GetUser(null, model.Email!);

        if (!userResponse.IsSuccessStatusCode ||
            userResponse.Content == null)
        {
            // user does not exist
            // redirect to error page
            return RedirectToAction(nameof(RecMemberNotFound), routeValues: new { recId = model.RecId.ToString() });
        }

        var reviewBodyResponse = await reviewBodyService.GetReviewBodyById(model.RecId);

        if (!reviewBodyResponse.IsSuccessStatusCode
            || reviewBodyResponse.Content == null)
        {
            // If the review body does not exist, return a 404 not found response
            return NotFound();
        }

        var userExists = reviewBodyResponse.Content.Users?.Any(u => u.Email == model.Email);

        if (userExists.GetValueOrDefault(false))
        {
            // user already in REC so redirect to error page
            return RedirectToAction(nameof(MemberExistsInRec), routeValues: new { recId = model.RecId.ToString() });
        }

        var isUserEnabled = userResponse.Content.User?.Status == IrasUserStatus.Active;

        if (!isUserEnabled)
        {
            // user is found but not active, redirect to error page
            return RedirectToAction(nameof(RecMemberNotActive), routeValues: new { recId = model.RecId.ToString() });
        }

        var userModel = userResponse.Content.User;

        model.Users = new List<UserViewModel> {
            new UserViewModel
            {
                Id = userModel!.Id,
                GivenName = userModel.GivenName,
                FamilyName = userModel.FamilyName,
                Email = userModel.Email
            }
        };

        // user can be added so proceed to next screen
        return View(model);
    }

    [HttpPost]
    [CmsContentAction(nameof(AddRecMember))]
    public IActionResult EditNewRecMember(RecMemberViewModel model)
    {
        return View("AddRecMember", model);
    }

    [HttpGet]
    public async Task<IActionResult> CheckRecMember(Guid recId, Guid userId)
    {
        var reviewBodyResponse = await reviewBodyService.GetReviewBodyById(recId);

        if (!reviewBodyResponse.IsSuccessStatusCode ||
            reviewBodyResponse.Content == null)
        {
            // If the review body does not exist, throw error
            return this.ServiceError(reviewBodyResponse);
        }

        if (!await MemberManagementHelper.UserHasAccess(reviewBodyResponse.Content, User, userService))
        {
            // if user does not have access to the review body, return 403 forbidden
            return Forbid();
        }

        var recUser = reviewBodyResponse.Content?.Users?.FirstOrDefault(u => u.UserId == userId);

        if (recUser == null)
        {
            // user does not exist in the review body, return 404 not found
            return NotFound();
        }

        var userDetailsResponse = await userService.GetUser(userId.ToString(), null);

        if (!userDetailsResponse.IsSuccessStatusCode ||
            userDetailsResponse.Content == null)
        {
            // user does not exist, throw error
            return this.ServiceError(userDetailsResponse);
        }

        var model = PopulateRecMemberViewModel(userDetailsResponse.Content.User, reviewBodyResponse.Content!, recUser, true);

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> CheckRecMember(RecMemberViewModel model)
    {
        var isModelValid = await recMemberValidator.ValidateAsync(model);

        if (!isModelValid.IsValid)
        {
            foreach (var error in isModelValid.Errors)
            {
                ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
            }

            return View(nameof(AddRecMember), model);
        }

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> EditRecMember(Guid recId, Guid userId)
    {
        var reviewBodyResponse = await reviewBodyService.GetReviewBodyById(recId);

        if (!reviewBodyResponse.IsSuccessStatusCode ||
            reviewBodyResponse.Content == null)
        {
            // If the review body does not exist, throw error
            return this.ServiceError(reviewBodyResponse);
        }

        if (!await MemberManagementHelper.UserHasAccess(reviewBodyResponse.Content, User, userService))
        {
            // if user does not have access to the review body, return 403 forbidden
            return Forbid();
        }

        var recUser = reviewBodyResponse.Content.Users?.FirstOrDefault(u => u.UserId == userId);

        if (recUser == null)
        {
            // user does not exist in the review body, return 404 not found
            return NotFound();
        }

        var userDetailsResponse = await userService.GetUser(userId.ToString(), null);

        if (!userDetailsResponse.IsSuccessStatusCode ||
            userDetailsResponse.Content?.User == null)
        {
            // user does not exist, throw error
            return this.ServiceError(userDetailsResponse);
        }

        var model = PopulateRecMemberViewModel(userDetailsResponse.Content.User!, reviewBodyResponse.Content!, recUser, true);

        return View(nameof(AddRecMember), model);
    }

    [HttpGet]
    public async Task<IActionResult> AddRecMember(Guid recId, string userId)
    {
        var userProfileResponse = await userService.GetUser(userId, null);

        if (!userProfileResponse.IsSuccessStatusCode
            || userProfileResponse.Content == null)
        {
            return this.ServiceError(userProfileResponse);
        }

        var recResponse = await reviewBodyService.GetReviewBodyById(recId);

        if (!recResponse.IsSuccessStatusCode
            || recResponse.Content == null)
        {
            return this.ServiceError(recResponse);
        }

        if (!await MemberManagementHelper.UserHasAccess(recResponse.Content, User, userService))
        {
            // if user does not have access to the review body, return 403 forbidden
            return Forbid();
        }

        var userObject = userProfileResponse.Content.User;
        var model = new RecMemberViewModel
        {
            Title = userObject.Title,
            FirstName = userObject.GivenName,
            LastName = userObject.FamilyName,
            EmailAddress = userObject.Email,
            RecId = recId,
            RecName = recResponse.Content.RegulatoryBodyName,
            Organisation = userObject.Organisation,
            JobTitle = userObject.JobTitle,
            UserId = userId,
            IsEditMode = false
        };

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> AddRecMember(RecMemberViewModel model)
    {
        var isModelValid = await recMemberValidator.ValidateAsync(model);

        if (!isModelValid.IsValid)
        {
            foreach (var error in isModelValid.Errors)
            {
                ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
            }

            return View(model);
        }

        var reviewBodyUserDto = new ReviewBodyUserDto
        {
            UserId = Guid.Parse(model.UserId),
            Email = model.EmailAddress,
            Id = model.RecId,
            CommitteeRole = model.CommitteeRole,
            Designation = model.Designation,
            Telephone = model.RecTelephoneNumber,
            MemberLeftOrganisation = model.MemberLeftOrganisation,
            DateTimeLeft = model.DateTimeLeft
        };

        if (model.IsEditMode)
        {
            // this is editing an existing user so update them
            reviewBodyUserDto.DateTimeLastUpdated = DateTime.UtcNow;
            var updateUserResponse = await reviewBodyService.UpdateReviewBodyUser(reviewBodyUserDto);

            if (!updateUserResponse.IsSuccessStatusCode)
            {
                return this.ServiceError(updateUserResponse);
            }

            TempData[TempDataKeys.ShowNotificationBanner] = true;
            return RedirectToAction(nameof(CheckRecMember), new { recId = model.RecId, userId = model.UserId });
        }
        else
        {
            // this is a new user so add them

            // check if they already exist in rec
            var reviewBodyResponse = await reviewBodyService.GetReviewBodyById(model.RecId);

            if (!reviewBodyResponse.IsSuccessStatusCode ||
                reviewBodyResponse.Content == null)
            {
                return this.ServiceError(reviewBodyResponse);
            }

            var userExists = reviewBodyResponse.Content.Users?.Any(u => u.Email == model.EmailAddress);

            if (userExists.GetValueOrDefault(false))
            {
                // user already in REC so redirect to error page
                return RedirectToAction(nameof(MemberExistsInRec), routeValues: new { recId = model.RecId.ToString() });
            }

            var userResponse = await userService.GetUser(null, model.EmailAddress);

            if (!userResponse.IsSuccessStatusCode ||
                userResponse.Content == null)
            {
                return this.ServiceError(userResponse);
            }

            var isUserEnabled = userResponse.Content.User?.Status == IrasUserStatus.Active;

            if (!isUserEnabled)
            {
                // user is found but not active, redirect to error page
                return RedirectToAction(nameof(RecMemberNotActive), routeValues: new { recId = model.RecId.ToString() });
            }

            // add user is good to be added to REC
            reviewBodyUserDto.DateAdded = DateTime.UtcNow;
            var addUserToRec = await reviewBodyService.AddUserToReviewBody(reviewBodyUserDto);

            if (!addUserToRec.IsSuccessStatusCode)
            {
                return this.ServiceError(addUserToRec);
            }

            // TODO change to Users list page once present
            TempData[TempDataKeys.ShowNotificationBanner] = true;
            return RedirectToAction(nameof(ResearchEthicsCommitteeMembers), new { id = model.RecId });
        }
    }

    [HttpGet]
    public IActionResult MemberExistsInRec(string recId)
    {
        return View("MemberExistsInRec", recId);
    }

    [HttpGet]
    public IActionResult RecMemberNotFound(string recId)
    {
        return View("RecMemberNotFound", recId);
    }

    [HttpGet]
    public IActionResult RecMemberNotActive(string recId)
    {
        return View("RecMemberNotActive", recId);
    }

    private RecMemberViewModel PopulateRecMemberViewModel(User userDetails, ReviewBodyDto rec, ReviewBodyUserDto recMember, bool isEditMode)
    {
        var model = new RecMemberViewModel
        {
            RecId = rec.Id,
            UserId = userDetails.Id!,
            Title = userDetails.Title,
            FirstName = userDetails?.GivenName,
            LastName = userDetails?.FamilyName,
            EmailAddress = userDetails?.Email,
            Organisation = userDetails?.Organisation,
            JobTitle = userDetails?.JobTitle,
            RecName = rec.RegulatoryBodyName,
            IsEditMode = isEditMode,
            CommitteeRole = recMember.CommitteeRole,
            Designation = recMember.Designation,
            RecTelephoneNumber = recMember.Telephone,
            MemberLeftOrganisation = recMember.MemberLeftOrganisation,
            LastUpdated = recMember.DateTimeLastUpdated,
            DateTimeLeftDay = recMember.DateTimeLeft?.Day.ToString(),
            DateTimeLeftMonth = recMember.DateTimeLeft?.Month.ToString(),
            DateTimeLeftYear = recMember.DateTimeLeft?.Year.ToString()
        };

        return model;
    }

    /// <summary>
    ///     Displays a Research Ethics Committee Members
    /// </summary>
    [Authorize(Policy = Permissions.MemberManagement.ResearchEthicsCommittees_ManageMembers)]
    [HttpGet]
    public async Task<IActionResult> ResearchEthicsCommitteeMembers(Guid id, string? sortField, string? sortDirection)
    {
        var recProfile = await reviewBodyService.GetReviewBodyById(id);

        if (!recProfile.IsSuccessStatusCode ||
                recProfile.Content == null)
        {
            return this.ServiceError(recProfile);
        }

        if (!await MemberManagementHelper.UserHasAccess(recProfile.Content, User, userService))
        {
            // if user does not have access to the review body, return 403 forbidden
            return Forbid();
        }

        var model = new RecMembersViewModel()
        {
            RecId = recProfile.Content?.Id,
            RecName = recProfile.Content?.RegulatoryBodyName
        };

        // pagination data not needed - only sorting
        model.Pagination = new PaginationViewModel(0, 0, 0)
        {
            SortField = sortField,
            SortDirection = sortDirection
        };

        // retrieving and mapping users data
        var recUsers = recProfile.Content?.Users;

        if (recUsers is not null)
        {
            var tempRecUsers = new List<RecMemberViewModel>();

            var usersDetails = await userService.GetUsersByIds(recUsers.Select(x => x.UserId.ToString()), null, 1, int.MaxValue);

            if (!usersDetails.IsSuccessStatusCode || usersDetails.Content == null)
            {
                // user does not exist, throw error
                return this.ServiceError(usersDetails);
            }

            foreach (var userDetails in usersDetails.Content.Users)
            {
                tempRecUsers.Add(PopulateRecMemberViewModel(userDetails, recProfile.Content!, recUsers.First(x => x.UserId.ToString().Equals(userDetails.Id, StringComparison.CurrentCultureIgnoreCase)), false));
            }

            tempRecUsers = SortMembers(sortField, sortDirection, tempRecUsers);

            model.RecUsers = tempRecUsers;
        }

        return View(model);
    }

    /// <summary>
    ///     Displays a Research Ethics Committee Profile Audit History
    /// </summary>
    [Route("/membermanagement/researchethicscommitteeaudit", Name = "mm:recaudithistory")]
    [Authorize(Policy = Permissions.MemberManagement.ResearchEthicsCommittees_Access)]
    [HttpGet]
    public async Task<IActionResult> RecProfileAuditHistory(Guid recId, string? sortField, string? sortDirection, int pageSize = 20, int pageNumber = 1)
    {
        var skip = (pageNumber - 1) * pageSize;
        var response = await reviewBodyService.ReviewBodyAuditTrail(recId, skip, pageSize);

        var auditTrailResponse = response?.Content;
        var items = auditTrailResponse?.Items;

        if (items is not null && items.Any())
        {
            items = SortAuditEntries(sortField, sortDirection, items!.ToList());
        }

        var paginationModel = new PaginationViewModel(pageNumber, pageSize, auditTrailResponse != null ? auditTrailResponse.TotalCount : -1)
        {
            SortField = sortField,
            SortDirection = sortDirection,
            RouteName = "mm:recaudithistory",
            AdditionalParameters =
                {
                    { "recId", recId.ToString() }
                }
        };

        var reviewBody = await reviewBodyService.GetReviewBodyById(recId);
        var reviewBodyName = reviewBody?.Content?.RegulatoryBodyName;

        var resultModel = new ReviewBodyAuditTrailViewModel
        {
            BodyName = reviewBodyName,
            Pagination = paginationModel,
            Items = items!
        };

        return View(resultModel);
    }

    
    [HttpGet]
    public async Task<IActionResult> RecMemberAuditHistory
    (
        Guid recId,
        Guid userId,
        int pageNumber = 1,
        int pageSize = 10,
        string sortField = nameof(RecMemberAuditHistoryEntryViewModel.DateTimeStamp),
        string sortDirection = SortDirections.Descending
    )
    {
        var recProfile = await reviewBodyService.GetReviewBodyById(recId);

        if (!recProfile.IsSuccessStatusCode || recProfile.Content == null)
        {
            return this.ServiceError(recProfile);
        }

        var recUser = await userService.GetUser(userId.ToString(), null);

        if (!recUser.IsSuccessStatusCode || recUser.Content == null)
        {
            return this.ServiceError(recUser);
        }

        var auditTrailResponse =
            await reviewBodyService.ReviewBodyUserAuditTrail(recId, userId, (pageNumber - 1) * pageSize, pageSize, sortField, sortDirection);

        if (!auditTrailResponse.IsSuccessStatusCode || auditTrailResponse.Content == null)
        {
            return this.ServiceError(auditTrailResponse);
        }

        var model = new RecMemberAuditHistoryViewModel
        {
            ReviewBody = recProfile.Content,
            AuditHistoryEntries = [.. auditTrailResponse.Content.Items.Select(at => new RecMemberAuditHistoryEntryViewModel
            {
                DateTimeStamp = at.DateTimeStamp,
                Description = at.Description,
                User = at.User
            })],
            User = recUser.Content.User,
            Pagination = new PaginationViewModel(pageNumber, pageSize, auditTrailResponse.Content.TotalCount)
            {
                SortField = sortField,
                SortDirection = sortDirection,
                AdditionalParameters = new Dictionary<string, string>
                {
                    { "recId", recId.ToString() },
                    { "userId", userId.ToString() }
                }
            }
        };

        return View(model);
    }

    private static readonly Dictionary<string, Func<RecMemberViewModel, string?>> SortMembersSelectors =
    new()
    {
        [nameof(RecMemberViewModel.FirstName)] = u => u.FirstName,
        [nameof(RecMemberViewModel.LastName)] = u => u.LastName,
        [nameof(RecMemberViewModel.EmailAddress)] = u => u.EmailAddress,
        [nameof(RecMemberViewModel.CommitteeRole)] = u => u.CommitteeRole,
        [nameof(RecMemberViewModel.Designation)] = u => u.Designation
    };

    private static readonly Dictionary<string, Func<ReviewBodyAuditTrailDto, string?>> SortAuditEntriesSelectors =
    new()
    {
        [nameof(ReviewBodyAuditTrailDto.DateTimeStamp)] = u => u.DateTimeStamp.ToString(),
        [nameof(ReviewBodyAuditTrailDto.Description)] = u => u.Description
    };

    private static List<RecMemberViewModel> SortMembers(
    string? sortField,
    string? sortDirection,
    List<RecMemberViewModel> users)
    {
        if (string.IsNullOrEmpty(sortField))
        {
            return users;
        }

        if (!SortMembersSelectors.TryGetValue(sortField, out var selector))
        {
            return users;
        }

        var ascending = sortDirection == SortDirections.Ascending;

        return ascending
            ? users.OrderBy(selector).ToList()
            : users.OrderByDescending(selector).ToList();
    }

    private static List<ReviewBodyAuditTrailDto> SortAuditEntries(
    string? sortField,
    string? sortDirection,
    List<ReviewBodyAuditTrailDto> entries)
    {
        if (string.IsNullOrEmpty(sortField))
        {
            return entries;
        }

        if (!SortAuditEntriesSelectors.TryGetValue(sortField, out var selector))
        {
            return entries;
        }

        var ascending = sortDirection == SortDirections.Ascending;

        return ascending
            ? entries.OrderBy(selector).ToList()
            : entries.OrderByDescending(selector).ToList();
    }
}