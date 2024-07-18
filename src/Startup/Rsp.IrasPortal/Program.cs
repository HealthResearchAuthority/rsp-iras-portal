using Azure.Identity;
using HealthChecks.UI.Client;
using Rsp.IrasPortal.Application.Configuration;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Configuration.Auth;
using Rsp.IrasPortal.Configuration.Dependencies;
using Rsp.IrasPortal.Configuration.Health;
using Rsp.IrasPortal.Configuration.HttpClients;
using Rsp.Logging.Middlewares.CorrelationId;
using Rsp.Logging.Middlewares.RequestTracing;
using Rsp.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

//Add logger
builder
    .Configuration
    .AddJsonFile("logsettings.json")
    .AddEnvironmentVariables();

// this method is called by multiple projects
// serilog settings has been moved here, as all projects
// would need it
builder.AddServiceDefaults();

// Add services to the container.
var services = builder.Services;
var configuration = builder.Configuration;

if (!builder.Environment.IsDevelopment())
{
    var azureAppSettingsSection = configuration.GetSection(nameof(AppSettings));
    var azureAppSettings = azureAppSettingsSection.Get<AppSettings>()!;

    // Load configuration from Azure App Configuration
    builder.Configuration.AddAzureAppConfiguration(options =>
        options.Connect(
            new Uri(azureAppSettings!.AzureAppConfiguration.Endpoint),
            new ManagedIdentityCredential(azureAppSettings.AzureAppConfiguration.IdentityClientID)));

    builder.Services.Configure<AppSettings>(builder.Configuration.GetSection(nameof(AppSettings)));
}

var appSettingsSection = configuration.GetSection(nameof(AppSettings));
var appSettings = appSettingsSection.Get<AppSettings>()!;

services.AddSingleton(appSettings);

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
services.AddControllersWithViews();

// configure health checks to monitor
// microservice health
services.AddCustomHealthChecks(appSettings);

// header to be propagated to the httpclient
// to be sent in the request for external api calls
services.AddHeaderPropagation(options => options.Headers.Add(CustomRequestHeaders.CorrelationId));

services
    .AddJwksManager()
    .UseJwtValidation();

var app = builder.Build();

app.MapHealthChecks("/probes/liveness");

app.MapDefaultEndpoints();

app.UseStaticFiles(); // this will serve the static files from wwwroot folder

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Application/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
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
                endpoints.MapHealthChecks("/portal-health", new()
                {
                    Predicate = _ => true,
                    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
                });

                endpoints.MapHealthChecksUI();
                endpoints.MapControllers();
            }
        );

    app.UseJwksDiscovery();

    await app.RunAsync();