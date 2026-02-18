using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Rsp.Portal.Application.Constants;
using Rsp.Portal.Application.Extensions;
using Rsp.Portal.Application.Services;
using Rsp.Portal.Domain.AccessControl;

namespace Rsp.IrasPortal.Application.Filters;

public class ModificationAuthoriseFilter : IAsyncAuthorizationFilter, IAsyncActionFilter
{
    private readonly string _permission;
    private IProjectModificationsService _projectModificationsService;
    private ISponsorOrganisationService _sponsorOrganisationService;
    private ISponsorUserAuthorisationService _sponsorUserAuthorisationService;
    private ITempDataDictionaryFactory _tempDataFactory;
    private bool performCheckIfSponsorIsAuthoriserForRevision { get; set; } = false;

    public ModificationAuthoriseFilter
    (
        string permission,
        IProjectModificationsService projectModificationsService,
        ISponsorOrganisationService sponsorOrganisationService,
        ISponsorUserAuthorisationService sponsorUserAuthorisationService,
        ITempDataDictionaryFactory tempDataDictionaryFactory
    )
    {
        _permission = permission;
        _projectModificationsService = projectModificationsService;
        _sponsorOrganisationService = sponsorOrganisationService;
        _sponsorUserAuthorisationService = sponsorUserAuthorisationService;
        _tempDataFactory = tempDataDictionaryFactory;
    }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var user = context.HttpContext.User;

        if (user?.Identity?.IsAuthenticated != true)
        {
            context.Result = new ChallengeResult();
            return;
        }

        if (user.HasPermission(Permissions.Sponsor.Modifications_Authorise))
        {
            performCheckIfSponsorIsAuthoriserForRevision = true;
            return;
        }

        if (user.HasPermission(_permission))
        {
            return;
        }
        else
        {
            context.Result = new ForbidResult();
            return;
        }
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (context.Result != null)
        {
            return;
        }

        if (performCheckIfSponsorIsAuthoriserForRevision)
        {
            // 1. Get controller
            var controller = context.Controller as Controller;
            if (controller == null)
            {
                context.Result = new ForbidResult();
                return;
            }

            // 2. Get Ids from RouteData or TempData
            var tempData = _tempDataFactory.GetTempData(context.HttpContext);
            var actionName = context.ActionDescriptor.RouteValues["action"];

            string projectRecordId = string.Empty;
            Guid projectModificationIdGuid;
            Guid sponsorOrganisationUserIdGuid;
            string rtsId = string.Empty;

            if (actionName == "ModificationDetails")
            {
                if (!context.ActionArguments.TryGetValue("projectRecordId", out var projectRecordIdObj) ||
                    string.IsNullOrWhiteSpace(projectRecordIdObj?.ToString()) ||
                    !context.ActionArguments.TryGetValue("projectModificationId", out var projectModificationIdObj) ||
                    !Guid.TryParse(projectModificationIdObj?.ToString(), out projectModificationIdGuid)
                )
                {
                    context.Result = new BadRequestObjectResult("Missing or invalid query/route parameters.");
                    return;
                }
                projectRecordId = projectRecordIdObj?.ToString() ?? string.Empty;
                context.ActionArguments.TryGetValue("sponsorOrganisationUserId", out var sponsorOrganisationUserIdObj);
                Guid.TryParse(sponsorOrganisationUserIdObj?.ToString(), out sponsorOrganisationUserIdGuid);
                context.ActionArguments.TryGetValue("rtsId", out var rtsIdObj);
                rtsId = rtsIdObj?.ToString() ?? string.Empty;
            }
            else
            {
                projectModificationIdGuid = PeekGuid(tempData, TempDataKeys.ProjectModification.ProjectModificationId);
                sponsorOrganisationUserIdGuid = PeekGuid(tempData, TempDataKeys.RevisionSponsorOrganisationUserId);
                projectRecordId = tempData.Peek(TempDataKeys.ProjectRecordId)?.ToString() ?? string.Empty;
                rtsId = tempData.Peek(TempDataKeys.RevisionRtsId)?.ToString() ?? string.Empty;

                if (projectModificationIdGuid == Guid.Empty || projectRecordId == string.Empty)
                {
                    context.Result = new BadRequestObjectResult("Missing TempData identifiers.");
                    return;
                }
            }

            // 3. Check if modification is in ReviseAndAuthorise state
            var modificationResult = await _projectModificationsService.GetModification(projectRecordId, projectModificationIdGuid);

            if (modificationResult.Content?.Status is not ModificationStatus.ReviseAndAuthorise)
            {
                if (!context.HttpContext.User.HasPermission(_permission))
                {
                    context.Result = new ForbidResult();
                    return;
                }

                // user has permission (f.e. System Admin)
                await next();
                return;
            }

            if (sponsorOrganisationUserIdGuid == Guid.Empty || rtsId == string.Empty)
            {
                context.Result = new BadRequestObjectResult("Missing or invalid parameter.");
                return;
            }

            // 4. Check if current user is the one for provided sponsorOrganisationUserId & rtsId
            var auth = await _sponsorUserAuthorisationService.AuthoriseWithOrganisationContextAsync(controller, sponsorOrganisationUserIdGuid, context.HttpContext.User, rtsId);
            if (!auth.IsAuthorised)
            {
                context.Result = auth.FailureResult!;
                return;
            }

            // 5. Check if current user is Authoriser
            var sponsorOrganisationUser =
                await _sponsorOrganisationService.GetSponsorOrganisationUser(sponsorOrganisationUserIdGuid);

            if (!sponsorOrganisationUser.IsSuccessStatusCode)
            {
                context.Result = new ForbidResult();
                return;
            }

            tempData[TempDataKeys.IsAuthoriser] = sponsorOrganisationUser.Content!.IsAuthoriser;
            if (sponsorOrganisationUser.Content!.IsAuthoriser)
            {
                tempData[TempDataKeys.RevisionSponsorOrganisationUserId] = sponsorOrganisationUserIdGuid;
                tempData[TempDataKeys.RevisionRtsId] = rtsId;
            }
            else
            {
                context.Result = new ForbidResult();
                return;
            }
        }

        await next();
    }

    public static Guid PeekGuid(ITempDataDictionary tempData, string key)
    {
        var element = tempData.Peek(key);

        if (element != null)
        {
            return ((Guid)element);
        }

        return Guid.Empty;
    }
}