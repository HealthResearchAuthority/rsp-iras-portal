using Microsoft.AspNetCore.Mvc;

namespace Rsp.IrasPortal.Application.Services;

public interface IViewRenderService
{
    public Task<string> RenderViewAsString<TModel>(string viewName, TModel model, ControllerContext controllerContext);
}