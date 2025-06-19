using FluentValidation;
using Mapster;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.FeatureManagement;
using Microsoft.Extensions.Azure;
using Azure.Storage.Blobs;
using Rsp.IrasPortal.Application.Configuration;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Configuration.AppConfiguration;
using Rsp.IrasPortal.Configuration.Auth;
using Rsp.IrasPortal.Configuration.Dependencies;
using Rsp.IrasPortal.Configuration.Health;
using Rsp.IrasPortal.Configuration.HttpClients;
using Rsp.IrasPortal.Web;
using Rsp.IrasPortal.Web.Mapping;
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

services.AddHttpClients(appSettings!);

// routing configuration
services.AddRouting(options => options.LowercaseUrls = true);

if (await featureManager.IsEnabledAsync(Features.OneLogin))
{
    services.AddOneLoginAuthentication(appSettings);
}
else
{
    services.AddAuthenticationAndAuthorization(appSettings);
}

services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromSeconds(appSettings.SessionTimeout);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// add controllers and views
services
    .AddControllersWithViews(async options =>
    {
        options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
        if (await featureManager.IsEnabledAsync(Features.InterceptedLogging))
        {
            options.Filters.Add<LogActionFilter>();
        }
    })
    .AddSessionStateTempDataProvider();

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
    azure.AddBlobServiceClient(builder.Configuration.GetRequiredSection("AppSettings:Azure:DocumentStorage:Blob"));
});

services
    .AddJwksManager()
    .UseJwtValidation();

services.AddValidatorsFromAssemblyContaining<IWebApp>();

var config = TypeAdapterConfig.GlobalSettings;

// register the mapping configuration
config.Scan(typeof(MappingRegister).Assembly);

if (await featureManager.IsEnabledAsync(Features.InterceptedLogging))
{
    services.AddLoggingInterceptor<LoggingInterceptor>();
}

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

app.MapShortCircuit(StatusCodes.Status404NotFound, "robots.txt", "favicon.ico", "*.css");

app
    .UseRouting()
    .UseSession()
    .UseAuthentication()
    .UseAuthorization()
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