using Microsoft.AspNetCore.Mvc;

namespace Rsp.Portal.Web.Helpers;

public interface IViewHelper
{
    public Task<string> RenderViewAsString<TModel>(string viewName, TModel model, ControllerContext controllerContext);

    public Task<byte[]> GeneratePdf(string html, string footerText = "");
}