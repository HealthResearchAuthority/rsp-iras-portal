﻿using System.Security.Claims;
using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Application.DTOs.Requests;
using Rsp.IrasPortal.Application.DTOs.Responses;
using Rsp.IrasPortal.Application.Responses;
using Rsp.IrasPortal.Web.Models;
using Rsp.IrasPortal.Web.Validators.Helpers;

namespace Rsp.IrasPortal.Web.Extensions;

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
            Id = httpContext.Items[ContextItemKeys.RespondentId]?.ToString() ?? string.Empty,
            EmailAddress = httpContext.Items[ContextItemKeys.Email]?.ToString() ?? string.Empty,
            GivenName = httpContext.Items[ContextItemKeys.FirstName]?.ToString() ?? string.Empty,
            FamilyName = httpContext.Items[ContextItemKeys.LastName]?.ToString() ?? string.Empty,
            Role = string.Join(',', user.Claims
                       .Where(claim => claim.Type == ClaimTypes.Role)
                       .Select(claim => claim.Value))
        };
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

        if (!result.IsValid)
        {
            if (addModelErrors)
            {
                // Copy the validation results into ModelState.
                // ASP.NET uses the ModelState collection to populate
                // error messages in the View.
                foreach (var error in result.Errors)
                {
                    if (error.CustomState is QuestionViewModel qvm)
                    {
                        var adjustedPropertyName = PropertyNameHelper.AdjustPropertyName(error.PropertyName, qvm.Index);
                        controller.ModelState.AddModelError(adjustedPropertyName, error.ErrorMessage);
                    }
                    else
                    {
                        controller.ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
                    }
                }
            }

            return false;
        }

        return true;
    }
}