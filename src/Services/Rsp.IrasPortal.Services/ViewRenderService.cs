using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Rsp.IrasPortal.Application.Services;

namespace Rsp.IrasPortal.Services;

public class ViewRenderService : IViewRenderService
{
    public async Task<string> RenderViewAsString<TModel>(string viewName, TModel model, ControllerContext controllerContext)
    {
        var actionContext = controllerContext as ActionContext;
        var serviceProvider = controllerContext.HttpContext.RequestServices;

        if (serviceProvider.GetService(typeof(ITempDataProvider)) is not ITempDataProvider tempDataProvider)
        {
            throw new ArgumentNullException(nameof(controllerContext), "TempDataProvider is null.");
        }

        if (serviceProvider.GetService(typeof(IRazorViewEngine)) is not IRazorViewEngine razorViewEngine)
        {
            throw new ArgumentNullException(nameof(controllerContext), "RazorViewEngine is null.");
        }

        await using var stringWriter = new StringWriter();

        var viewResult = razorViewEngine.FindView(actionContext, viewName, false);

        if (viewResult.View == null)
        {
            throw new ArgumentNullException($"View '{viewName}' not found.");
        }

        var viewDictionary = new ViewDataDictionary<TModel>(
            new EmptyModelMetadataProvider(),
            new ModelStateDictionary())
        {
            Model = model
        };

        var tempDataDictionary = new TempDataDictionary(
            actionContext.HttpContext,
            tempDataProvider);

        var viewContext = new ViewContext(
            actionContext,
            viewResult.View,
            viewDictionary,
            tempDataDictionary,
            stringWriter,
            new HtmlHelperOptions()
        );

        await viewResult.View.RenderAsync(viewContext);

        var html = stringWriter.ToString();

        return InjectCss(html);
    }

    private static string InjectCss(string html)
    {
        var cssFiles = new[]
        {
            "wwwroot/css/govuk-frontend.min.css",
            "wwwroot/css/main.css"
        };

        var cssBuilder = new StringBuilder();

        foreach (var relativePath in cssFiles)
        {
            var fullPath = Path.Combine(Directory.GetCurrentDirectory(), relativePath);

            if (!File.Exists(fullPath))
                throw new FileNotFoundException($"CSS file not found: {fullPath}");

            cssBuilder.AppendLine(File.ReadAllText(fullPath));
        }

        var combinedCss = cssBuilder.ToString();

        var headMatch = Regex.Match(html, "<head.*?>", RegexOptions.IgnoreCase);

        return headMatch.Success ?
            html.Insert(headMatch.Index + headMatch.Length, $"\n<style>\n{combinedCss}\n</style>\n") :
            $"<style>\n{combinedCss}\n</style>\n{html}";
    }
}