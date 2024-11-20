using Azure.Identity;
using FluentValidation;
using GovUk.Frontend.AspNetCore;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.FeatureManagement;
using Rsp.IrasPortal.Application.Configuration;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Configuration.Auth;
using Rsp.IrasPortal.Configuration.Dependencies;
using Rsp.IrasPortal.Configuration.Health;
using Rsp.IrasPortal.Configuration.HttpClients;
using Rsp.IrasPortal.Web;
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

// this will use the FeatureManagement section
services.AddFeatureManagement();

if (!builder.Environment.IsDevelopment())
{
    var azureAppSettingsSection = configuration.GetSection(nameof(AppSettings));
    var azureAppSettings = azureAppSettingsSection.Get<AppSettings>()!;

    // Load configuration from Azure App Configuration
    builder.Configuration.AddAzureAppConfiguration
    (
        options =>
        {
            options.Connect
            (
                new Uri(azureAppSettings!.AzureAppConfiguration.Endpoint),
                new ManagedIdentityCredential(azureAppSettings.AzureAppConfiguration.IdentityClientID)
            )
            .Select(KeyFilter.Any) // select all the settings without any label
            .Select(KeyFilter.Any, AppSettings.ServiceLabel) // select all settings using the service name as label
            .ConfigureRefresh
            (
                refreshOptions =>
                {
                    // Sentinel is a special key, that is registered to monitor the change
                    // when this key is updated all of the keys will updated if refreshAll is true, after the cache is expired
                    // this won't restart the application, instead uses the middleware i.e. UseAzureAppConfiguration to refresh the keys
                    // IOptionsSnapshot<T> can be used to inject in the constructor, so that we get the latest values for T
                    // without this key, we would need to register all the keys we would like to monitor
                    refreshOptions
                        .Register("AppSettings:Sentinel", AppSettings.ServiceLabel, refreshAll: true)
                        .SetCacheExpiration(new TimeSpan(0, 0, 15));
                }
            );

            // enable feature flags
            options.UseFeatureFlags
            (
                featureFlagOptions =>
                {
                    featureFlagOptions
                        .Select(KeyFilter.Any) // select all flags without any label
                        .Select(KeyFilter.Any, AppSettings.ServiceLabel) // select all flags using the service name as label
                        .CacheExpirationInterval = TimeSpan.FromSeconds(15);
                }
            );
        }
    );

    builder.Services.AddAzureAppConfiguration();
}

builder.Services.Configure<AppSettings>(builder.Configuration.GetSection(nameof(AppSettings)));

var appSettingsSection = configuration.GetSection(nameof(AppSettings));
var appSettings = appSettingsSection.Get<AppSettings>()!;

// Add services to IoC container
services.AddServices();

services.AddHttpContextAccessor();

services.AddHttpClients(appSettings!);

// routing configuration
services.AddRouting(options => options.LowercaseUrls = true);

services.AddAuthenticationAndAuthorization(appSettings);

services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(15);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// add controllers and views
services
    .AddControllersWithViews()
    .AddSessionStateTempDataProvider();

services.Configure<HealthCheckPublisherOptions>(options => options.Period = TimeSpan.FromSeconds(300));

// configure health checks to monitor
// microservice health
services.AddCustomHealthChecks(appSettings);

// header to be propagated to the httpclient
// to be sent in the request for external api calls
services.AddHeaderPropagation(options => options.Headers.Add(RequestHeadersKeys.CorrelationId));

services
    .AddJwksManager()
    .UseJwtValidation();

services.AddGovUkFrontend();

services.AddValidatorsFromAssemblyContaining<IWebApp>();

var app = builder.Build();

app.MapDefaultEndpoints();

app.UseStaticFiles(); // this will serve the static files from wwwroot folder

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Application/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();

    app.UseAzureAppConfiguration();
}
else
{
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();

app.UseCorrelationId();

app.UseHeaderPropagation();

// uses the SerilogRequestLogging middleware
// see the overloads to provide options for
// message template for request
app.UseRequestTracing();

app.MapShortCircuit(404, "robots.txt", "favicon.ico", "*.css");

app
    .UseRouting()
    .UseAuthentication()
    .UseAuthorization()
    .UseSession()
    .UseEndpoints
    (
        endpoints =>
        {
            endpoints.MapHealthChecks("/probes/liveness");
            endpoints.MapControllers();
        }
);

app.UseJwksDiscovery();

await app.RunAsync();