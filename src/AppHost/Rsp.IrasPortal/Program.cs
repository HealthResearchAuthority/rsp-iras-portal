using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Rsp.IrasPortal.Application.Configuration;
using Rsp.IrasPortal.Application.ServiceClients;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.HttpClients;
using Rsp.IrasPortal.Infrastructure.ServiceClients;
using Rsp.IrasPortal.Services;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

//Add logger
builder
    .Configuration
    .AddJsonFile("logsettings.json");

builder
    .Host
    .UseSerilog((host, logger) => logger.ReadFrom.Configuration(host.Configuration));

var settings = builder.Configuration.GetSection(nameof(AppSettings)).Get<AppSettings>();

// Add services to the container.
var services = builder.Services;

// add application services
services.AddTransient<ICategoriesService, CategoriesService>();

// add microservice clients
services.AddTransient<ICategoriesServiceClient, CategoriesServiceClient>();

services.AddHttpClients(settings!);

// add controllers and views
services.AddControllersWithViews();

// configure health checks to monitor
// microservice health
//var uri = new Uri(settings.CategoriesServiceUri!, "/health");

//services
//    .AddHealthChecks()
//    .AddUrlGroup(uri, "Categories API", HealthStatus.Unhealthy);

//services
//    .AddHealthChecksUI(opt =>
//    {
//        opt.SetEvaluationTimeInSeconds(10); //time in seconds between check
//        opt.MaximumHistoryEntriesPerEndpoint(60); //maximum history of checks
//        opt.SetApiMaxActiveRequests(1); //api requests concurrency
//        opt.AddHealthCheckEndpoint("Health Status", "/health"); //map health check api
//    }).AddInMemoryStorage();

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
})
.AddCookie()
.AddOpenIdConnect(options =>
{
    options.Authority = "https://dev.id.nihr.ac.uk/oauth2/token";
    options.ClientId = "[ClientID]";
    options.ClientSecret = "[ClientSecret]";
    options.ResponseType = OpenIdConnectResponseType.Code;
    options.SaveTokens = true;
    options.Scope.Add("openid");
    options.Scope.Add("profile");
    options.CallbackPath = "/signin-oidc"; // Default callback path
});

var app = builder.Build();

app.UseStaticFiles(); // this will serve the static files from wwwroot folder

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Application/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app
    .UseRouting()
    .UseAuthentication()
    .UseAuthorization();
//.UseEndpoints(config =>
//{
//    config.MapHealthChecks("/health",
//    new HealthCheckOptions()
//    {
//        Predicate = _ => true,
//        ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
//    });
//    config.MapHealthChecksUI();
//});

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Application}/{action=Welcome}/{id?}");

app.Run();