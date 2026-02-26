using System.Security.Claims;
using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Rsp.Portal.Application.Constants;
using Rsp.Portal.Application.DTOs.Requests;
using Rsp.Portal.Application.DTOs.Responses;
using Rsp.Portal.Application.Responses;
using Rsp.Portal.Application.Services;
using Rsp.Portal.Web.Models;
using Rsp.Portal.Web.Validators.Helpers;

namespace Rsp.Portal.Web.Extensions;

public static class ControllerExtensions
{
    /// <summary>
    /// Returns an appropriate IActionResult based on the ServiceResponse status code.
    /// If Forbidden or NotFound, returns Forbid or NotFound result.
    /// Otherwise, returns the generic error view with problem details.
    /// </summary>
    public static IActionResult ServiceError<T>(this Controller controller, ServiceResponse<T> response)
    {
        // Store it in HttpContext so ErrorController can read it later
        controller.HttpContext.Items[ContextItemKeys.ProblemDetails] = ProblemResult(controller, response);

        // UseStatusCodePagesWithExecute will redirect to /error/statuscode
        return controller.StatusCode((int)response.StatusCode);
    }

    /// <summary>
    /// Returns an appropriate IActionResult based on the ServiceResponse status code.
    /// If Forbidden or NotFound, returns Forbid or NotFound result.
    /// Otherwise, returns the generic error view with problem details.
    /// </summary>
    public static IActionResult ServiceError(this Controller controller, ServiceResponse response)
    {
        // Store it in HttpContext so ErrorController can read it later
        controller.HttpContext.Items[ContextItemKeys.ProblemDetails] = ProblemResult(controller, response);

        // UseStatusCodePagesWithExecute will redirect to /error/handlestatuscode
        return controller.StatusCode((int)response.StatusCode);
    }

    /// <summary>
    /// Retrieves the IrasApplicationResponse object from the session.
    /// Returns a new IrasApplicationResponse if not found in session.
    /// </summary>
    public static IrasApplicationResponse GetApplicationFromSession(this Controller controller)
    {
        var context = controller.HttpContext;

        var application = context.Session.GetString(SessionKeys.ProjectRecord);

        if (application != null)
        {
            return JsonSerializer.Deserialize<IrasApplicationResponse>(application)!;
        }

        return new IrasApplicationResponse();
    }

    /// <summary>
    /// Creates a ProblemDetails object from a ServiceResponse.
    /// Used for error reporting in views.
    /// </summary>
    public static ProblemDetails ProblemResult(this Controller controller, ServiceResponse response)
    {
        return new ProblemDetails
        {
            Title = response.ReasonPhrase,
            Detail = response.Error,
            Status = (int)response.StatusCode,
            Instance = controller.Request?.Path
        };
    }

    /// <summary>
    /// Extracts respondent information from the current HttpContext and user claims.
    /// </summary>
    public static RespondentDto GetRespondentFromContext(this Controller controller)
    {
        var httpContext = controller.HttpContext;
        var user = controller.User;

        return new RespondentDto
        {
            Id = httpContext.Items[ContextItemKeys.UserId]?.ToString() ?? string.Empty,
            EmailAddress = httpContext.Items[ContextItemKeys.Email]?.ToString() ?? string.Empty,
            GivenName = httpContext.Items[ContextItemKeys.FirstName]?.ToString() ?? string.Empty,
            FamilyName = httpContext.Items[ContextItemKeys.LastName]?.ToString() ?? string.Empty,
            Role = string.Join(',', user.Claims
                       .Where(claim => claim.Type == ClaimTypes.Role)
                       .Select(claim => claim.Value))
        };
    }

    public static string GetUserIdFromContext(this Controller controller)
    {
        var httpContext = controller.HttpContext;
        return httpContext.Items[ContextItemKeys.UserId]?.ToString() ?? string.Empty;
    }

    /// <summary>
    /// Validates the passed QuestionnaireViewModel and return ture or false
    /// </summary>
    /// <param name="model"><see cref="QuestionnaireViewModel"/> to validate</param>
    public static async Task<bool> ValidateQuestionnaire
    (
        this Controller controller,
        IValidator<QuestionnaireViewModel> validator,
        QuestionnaireViewModel model,
        bool validateMandatory = false,
        bool addModelErrors = true
    )
    {
        // using the FluentValidation, create a new context for the model
        var context = new ValidationContext<QuestionnaireViewModel>(model);

        if (validateMandatory)
        {
            context.RootContextData["ValidateMandatoryOnly"] = true;
        }

        // this is required to get the questions in the validator
        // before the validation cicks in
        context.RootContextData["questions"] = model.Questions;

        // call the ValidateAsync to execute the validation
        // this will trigger the fluentvalidation using the injected validator if configured
        var result = await validator.ValidateAsync(context);

        if (!result.IsValid && addModelErrors)
        {
            // We cannot safely remove/add ModelState entries inside the raw error loop
            // because FluentValidation may return multiple failures for the SAME property.
            // Repeated Remove + Add on the same key in one pass can corrupt the internal
            // ModelState error collection and cause:
            // - Index was outside the bounds of the array
            // - Destination array was not long enough
            //
            // So we first GROUP errors by their final (adjusted) property key,
            // then mutate ModelState only ONCE per key.
            var errorMap = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

            foreach (var error in result.Errors)
            {
                string key;

                if (error.CustomState is QuestionViewModel qvm)
                {
                    key = PropertyNameHelper.AdjustPropertyName(error.PropertyName, qvm.Index);
                }
                else
                {
                    // Fallback to the original property name if no custom state is provided
                    key = error.PropertyName;
                }

                // This prevents duplicate ModelState mutations for the same key.
                if (!errorMap.TryGetValue(key, out var messages))
                {
                    messages = new List<string>();
                    errorMap[key] = messages;
                }

                messages.Add(error.ErrorMessage);
            }

            // Now safely update ModelState once per key
            foreach (var kvp in errorMap)
            {
                var modelStateKey = kvp.Key;
                var messages = kvp.Value;

                // Remove the existing entry only ONCE per key.
                // Remove() is safe even if the key does not exist.
                controller.ModelState.Remove(modelStateKey);

                // Add all validation messages for this key.
                // ModelState supports multiple errors per field, so we append them
                // instead of overwriting or re-removing the key.
                foreach (var message in messages)
                {
                    controller.ModelState.AddModelError(modelStateKey, message);
                }
            }

            return false;
        }

        return true;
    }

    public static async Task<IActionResult> HandleOrganisationSearchAsync(
        this Controller controller,
        IRtsService rtsService,
        ApprovalsSearchViewModel model,
        string jsonSessionVariableName,
        string? role,
        int? pageSize,
        int pageIndex)
    {
        var tempData = controller.TempData;
        var modelState = controller.ModelState;
        var httpContext = controller.HttpContext;

        var returnUrl = tempData.Peek(TempDataKeys.OrgSearchReturnUrl) as string;

        // store the irasId in the TempData to get in the view
        tempData.TryAdd(TempDataKeys.IrasId, model.Search.IrasId);

        // set the previous, current and next stages
        tempData.TryAdd(TempDataKeys.SponsorOrgSearched, "searched:true");

        // when search is performed, empty the currently selected organisation
        model.Search.SponsorOrgSearch.SelectedOrganisation = string.Empty;
        tempData.TryAdd(TempDataKeys.OrgSearch, model.Search.SponsorOrgSearch, true);

        if (string.IsNullOrEmpty(model.Search.SponsorOrgSearch.SearchText) || model.Search.SponsorOrgSearch.SearchText.Length < 3)
        {
            modelState.AddModelError("sponsor_org_search",
                "Enter at least 3 characters to search");

            // save the model state in temp data, to use it on redirects to show validation errors
            // the modelstate will be merged using the action filter ModelStateMergeAttribute
            // only if the TempData has ModelState stored
            tempData.TryAdd(TempDataKeys.ModelState, modelState.ToDictionary(), true);

            // Return the view with the model state errors.
            return controller.Redirect(returnUrl!);
        }

        // Use the default sponsor role if no role is provided.
        role ??= OrganisationRoles.Sponsor;

        var searchResponse = await rtsService.GetOrganisationsByName(model.Search.SponsorOrgSearch.SearchText, role, pageIndex, pageSize);

        if (!searchResponse.IsSuccessStatusCode || searchResponse.Content == null)
        {
            return controller.ServiceError(searchResponse);
        }

        var sponsorOrganisations = searchResponse.Content;

        tempData.TryAdd(TempDataKeys.SponsorOrganisations, sponsorOrganisations, true);

        //THIS IS ONLY USED HERE TO NOT SHOW THE FILTERS IF WE RUN A NON JAVASCRIPT ORG SEARCH
        model.Search.IgnoreFilters = true;

        httpContext.Session.SetString(jsonSessionVariableName, JsonSerializer.Serialize(model.Search));

        return controller.Redirect(returnUrl!);
    }

    public static void RemoveFilters(
        this Controller controller,
        string jsonSessionVariableName,
        ApprovalsSearchModel search,
        string key,
        string? value)
    {
        var httpContext = controller.HttpContext;

        var keyNormalized = key?.ToLowerInvariant().Replace(" ", "");

        switch (keyNormalized)
        {
            case "chiefinvestigatorname":
                search.ChiefInvestigatorName = null;
                break;

            case "shortprojecttitle":
                search.ShortProjectTitle = null;
                break;

            case "sponsororganisation":
                search.SponsorOrganisation = null;
                search.SponsorOrgSearch = new OrganisationSearchViewModel();
                break;

            case "datesubmitted":
                search.FromDay = search.FromMonth = search.FromYear = null;
                search.ToDay = search.ToMonth = search.ToYear = null;
                break;

            case "datesubmitted-from":
                search.FromDay = search.FromMonth = search.FromYear = null;
                break;

            case "datesubmitted-to":
                search.ToDay = search.ToMonth = search.ToYear = null;
                break;

            case "leadnation":
                if (!string.IsNullOrEmpty(value) && search.LeadNation?.Count > 0)
                {
                    search.LeadNation = search.LeadNation
                        .Where(c => !string.Equals(c, value, StringComparison.OrdinalIgnoreCase))
                        .ToList();
                }
                break;

            case "participatingnation":
                if (!string.IsNullOrEmpty(value) && search.ParticipatingNation?.Count > 0)
                {
                    search.ParticipatingNation = search.ParticipatingNation
                        .Where(c => !string.Equals(c, value, StringComparison.OrdinalIgnoreCase))
                        .ToList();
                }
                break;

            case "modificationtype":
                if (!string.IsNullOrEmpty(value) && search.ModificationTypes?.Count > 0)
                {
                    search.ModificationTypes = search.ModificationTypes
                        .Where(m => !string.Equals(m, value, StringComparison.OrdinalIgnoreCase))
                        .ToList();
                }
                break;
        }

        httpContext.Session.SetString(jsonSessionVariableName, JsonSerializer.Serialize(search));
    }

    public static async Task<List<string>?> GetRelevantCountriesForUser(
        this Controller controller,
        IReviewBodyService reviewBodyService,
        IUserManagementService userManagementService)
    {
        var leadNation = new List<string>();

        if (!Guid.TryParse(controller.User?.FindFirstValue(CustomClaimTypes.UserId), out var userId))
        {
            // userId does not exist so exit block
            leadNation.Add(UkCountryNames.England);
            return leadNation;
        }

        if (controller.User.IsInRole(Roles.SystemAdministrator))
        {
            // user is admin so they can see modifications for all contries
            leadNation = UkCountryNames.Countries;
        }
        else if (controller.User.IsInRole(Roles.TeamManager))
        {
            // if user is team manager, then take their assigned country into account
            var userEntity = await userManagementService.GetUser(userId.ToString(), null);
            leadNation = userEntity?.Content?.User?.Country != null ?
                userEntity?.Content?.User?.Country?.Split(',')?.ToList() :
                leadNation;
        }
        else
        {
            // if user is not team manager, then take their assigned review body into account if applicable
            var bodiesResp = await reviewBodyService.GetUserReviewBodies(userId);

            var reviewBodies = bodiesResp.IsSuccessStatusCode
                ? bodiesResp.Content
                : null;

            if (reviewBodies != null)
            {
                foreach (var body in reviewBodies)
                {
                    var rbResp = await reviewBodyService.GetReviewBodyById(body.Id);

                    if (rbResp?.Content?.Countries != null)
                    {
                        leadNation.AddRange(rbResp.Content.Countries);
                    }
                }
            }
        }

        return leadNation;
    }
}