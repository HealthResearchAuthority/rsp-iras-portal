using FluentValidation;
using Mapster;
using Microsoft.AspNetCore.CookiePolicy;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.FeatureManagement;
using Rsp.Portal.Application.Configuration;
using Rsp.Portal.Application.Constants;
using Rsp.Portal.Application.Middleware;
using Rsp.Portal.Configuration.AppConfiguration;
using Rsp.Portal.Configuration.Auth;
using Rsp.Portal.Configuration.Dependencies;
using Rsp.Portal.Configuration.FeatureFolders;
using Rsp.Portal.Configuration.Health;
using Rsp.Portal.Configuration.HttpClients;
using Rsp.Portal.Infrastructure.ExceptionHandlers;
using Rsp.Portal.Web;
using Rsp.Portal.Web.ActionFilters;
using Rsp.Portal.Web.Attributes;
using Rsp.Portal.Web.Mapping;
using Rsp.Logging.ActionFilters;
using Rsp.Logging.Extensions;
using Rsp.Logging.Interceptors;
using Rsp.Logging.Middlewares.CorrelationId;
using Rsp.Logging.Middlewares.RequestTracing;
using Rsp.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

//Add logger
builder
    .Configuration
    .AddJsonFile("logsettings.json")
    .AddJsonFile("featuresettings.json", true, true)
    .AddEnvironmentVariables();

// this method is called by multiple projects
// serilog settings has been moved here, as all projects
// would need it
builder.AddServiceDefaults();

// Add services to the container.
var services = builder.Services;
var configuration = builder.Configuration;

// Allow uploads up to 200 MB
services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 200 * 1024 * 1024; // 200 MB
    options.ValueCountLimit = int.MaxValue;
});

// Also configure Kestrel (important for large uploads)
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 200 * 1024 * 1024; // 200 MB
});

// this will use the FeatureManagement section
services.AddFeatureManagement();

if (!builder.Environment.IsDevelopment())
{
    services.AddAzureAppConfiguration(configuration);
}

builder.Services.Configure<AppSettings>(builder.Configuration.GetSection(nameof(AppSettings)));

var appSettingsSection = configuration.GetSection(nameof(AppSettings));
var appSettings = appSettingsSection.Get<AppSettings>()!;

services.AddSingleton(appSettings);

// Creating a feature manager without the use of DI. Injecting IFeatureManager
// via DI is appropriate in consturctor methods. At the startup, it's
// not recommended to call services.BuildServiceProvider and retreive IFeatureManager
// via provider. Instead, the follwing approach is recommended by creating FeatureManager
// with ConfigurationFeatureDefinitionProvider using the existing configuration.
var featureManager = new FeatureManager(new ConfigurationFeatureDefinitionProvider(configuration));

// Add services to IoC container
services.AddServices();

services.AddHttpContextAccessor();

services.AddHttpClients(appSettings!, builder.Environment);

// routing configuration
services.AddRouting(options => options.LowercaseUrls = true);

if (await featureManager.IsEnabledAsync(FeatureFlags.OneLogin))
{
    services.AddOneLoginAuthentication(appSettings);
}
else
{
    services.AddAuthenticationAndAuthorization(appSettings);
}

services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromSeconds(appSettings.SessionTimeout + 60);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var featureFolderOptions = new FeatureFolderOptions();

services
    .AddControllersWithViews(async options =>
    {
        options.Conventions.Add(new FeatureControllerModelConvention(featureFolderOptions));
        options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;

        if (await featureManager.IsEnabledAsync(FeatureFlags.InterceptedLogging))
        {
            options.Filters.Add<LogActionFilter>();
        }

        options.Filters.Add<ModelStateMergeAttribute>();

        // Map controllers to their filter session keys
        options.Filters.Add(new AdvancedFiltersSessionFilter(
            new Dictionary<string, string[]>
            {
                { "Users", [SessionKeys.UsersSearch] },
                { "ReviewBody", [SessionKeys.ReviewBodiesSearch] },
                { "SponsorOrganisations", [SessionKeys.SponsorOrganisationsSearch] },
                { "Approvals", [SessionKeys.ApprovalsSearch] },
                { "ModificationsTasklist", [SessionKeys.ModificationsTasklist] },
                { "MyTasklist", [SessionKeys.MyTasklist] },
            }
        ));
    })
    .AddRazorOptions(options => options.ConfigureFeatureFolders(featureFolderOptions))
    .AddSessionStateTempDataProvider();

// Lift the MVC model-binding collection cap (default is 1024)
services.Configure<MvcOptions>(o => o.MaxModelBindingCollectionSize = int.MaxValue);

services.Configure<HealthCheckPublisherOptions>(options => options.Period = TimeSpan.FromSeconds(300));

// Increase the value count limit for FormOptions to allow
// for submitting of large forms (e.g. question sets)
services.Configure<FormOptions>(options =>
{
    options.ValueCountLimit = int.MaxValue;
});

// configure health checks to monitor
// microservice health
services.AddCustomHealthChecks(appSettings);

// header to be propagated to the httpclient
// to be sent in the request for external api calls
services.AddHeaderPropagation(options => options.Headers.Add(RequestHeadersKeys.CorrelationId));

services.AddAzureClients(azure =>
{
    azure.AddBlobServiceClient(
        builder.Configuration["AppSettings:DocumentStorage:CleanBlobConnectionString"]
    ).WithName("Clean");

    azure.AddBlobServiceClient(
        builder.Configuration["AppSettings:DocumentStorage:StagingBlobConnectionString"]
    ).WithName("Staging");
});

services
    .AddJwksManager()
    .UseJwtValidation();

services.AddValidatorsFromAssemblyContaining<IWebApp>();

var config = TypeAdapterConfig.GlobalSettings;

// register the mapping configuration
config.Scan(typeof(MappingRegister).Assembly);

if (await featureManager.IsEnabledAsync(FeatureFlags.InterceptedLogging))
{
    services.AddLoggingInterceptor<LoggingInterceptor>();
}

// If the "UseFrontDoor" feature is enabled, configure forwarded headers options
if (await featureManager.IsEnabledAsync(FeatureFlags.UseFrontDoor))
{
    // Configure ForwardedHeadersOptions to handle proxy headers
    services.Configure<ForwardedHeadersOptions>(options =>
    {
        // Specify which forwarded headers should be processed
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor |
                                  ForwardedHeaders.XForwardedProto |
                                  ForwardedHeaders.XForwardedHost;

        // Clear known networks and proxies to allow forwarding from any source
        options.KnownNetworks.Clear();
        options.KnownProxies.Clear();

        // Set allowed hosts from the AppSettings configuration, splitting by semicolon
        options.AllowedHosts = appSettings.AllowedHosts.Split(';', StringSplitOptions.RemoveEmptyEntries);
    });
}

// Global Exception Handler to log all unhandled requests
// As we have UseExceptionHandler call with path below
// It will call the Error action method to display a custom page
services.AddExceptionHandler<GlobalExceptionHandler>();

var app = builder.Build();

app.UseForwardedHeaders();

app.MapDefaultEndpoints();
app.UseCookiePolicy(
    new CookiePolicyOptions
    {
        Secure = CookieSecurePolicy.Always,
        HttpOnly = HttpOnlyPolicy.Always
    });

app.UseStaticFiles(); // this will serve the static files from wwwroot folder

app.Use(async (context, next) =>
{
    var headers = context.Response.Headers;

    headers.Referer = "no-referrer";
    headers.XFrameOptions = "DENY";
    headers.XContentTypeOptions = "nosniff";

    await next();
});

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    // this will enable the GlobalExceptionHandler to catch unhandled
    // exceptions and log then, after that it will be redirected to
    // /error/servererror to display a custom page
    app.UseExceptionHandler("/error/servererror");

    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();

    app.UseAzureAppConfiguration();
}
else
{
    app.UseDeveloperExceptionPage();
}

// we need custom page for other statuses as well e.g. like 404, 403
// Re-executes the pipeline for error status codes (e.g. 404/500) so a unified endpoint (/error/statuscode)
// can render a custom page while preserving the original status code and request context (no external redirect).
app.UseStatusCodePagesWithReExecute("/error/statuscode", "?statusCode={0}");

app.UseHttpsRedirection();

app.UseCorrelationId();

app.UseHeaderPropagation();

// uses the SerilogRequestLogging middleware
// see the overloads to provide options for
// message template for request
app.UseRequestTracing();

app.MapShortCircuit(StatusCodes.Status404NotFound, "robots.txt", "favicon.ico", "*.css");

app
    .UseRouting()
    .UseSession()
    .UseAuthentication()
    .UseAuthorization()
    .UseMiddleware<CompleteProfileMiddleware>() // middleware to check if new user needs to complete their profile
    .UseEndpoints
    (
        endpoints =>
        {
            endpoints.MapHealthChecks("/probes/liveness");
            endpoints.MapControllers();

            // Fallback route for CMS content
            endpoints.MapControllerRoute(
                name: "cms",
                pattern: "pages/{*slug}",
                defaults: new { controller = "CmsContent", action = "Index" }
            );
        }
);

app.UseJwksDiscovery();

await app.RunAsync();