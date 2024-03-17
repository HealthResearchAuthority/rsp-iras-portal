using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Rsp.WeatherForecast.Web.Models;

namespace Rsp.WeatherForecast.Web.Controllers;

public class OfficeController : Controller
{
    private readonly ILogger<OfficeController> _logger;

    public OfficeController(ILogger<OfficeController> logger)
    {
        _logger = logger;
    }

    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}