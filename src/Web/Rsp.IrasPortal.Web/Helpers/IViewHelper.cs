using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace Rsp.Portal.Web.Helpers;

public interface IViewHelper
{
    public Task<string> RenderViewAsString<TModel>(string viewName, TModel model, ControllerContext controllerContext, ViewDataDictionary? viewData = null);

    public Task<byte[]> GeneratePdf(string html, string footerText = "");
}