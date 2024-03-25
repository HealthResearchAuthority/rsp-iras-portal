using Microsoft.Extensions.Diagnostics.HealthChecks;
using Rsp.IrasPortal.Application.Configuration;
using Rsp.IrasPortal.Application.ServiceClients;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.HttpClients;
using Rsp.IrasPortal.Infrastructure.HealthChecks;
using Rsp.IrasPortal.Infrastructure.ServiceClients;
using Rsp.IrasPortal.Services;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

var services = builder.Services;

services
    .AddHealthChecks()
    .AddCheck<ServiceHealthCheck>("Categories Service Health Check", failureStatus: HealthStatus.Unhealthy);

//Add logger
builder
    .Configuration
    .AddJsonFile("logsettings.json");

builder
    .Host
    .UseSerilog((host, logger) => logger.ReadFrom.Configuration(host.Configuration));

var settings = builder.Configuration.GetSection(nameof(AppSettings)).Get<AppSettings>();

// Add services to the container.

// add application services
services.AddTransient<ICategoriesService, CategoriesService>();

// add microservice clients
services.AddTransient<ICategoriesServiceClient, CategoriesServiceClient>();

services.AddHttpClients(settings!);

// add controllers and views
services.AddControllersWithViews();

services.AddHealthChecksUI(opt =>
{
    opt.SetEvaluationTimeInSeconds(10); //time in seconds between check
    opt.MaximumHistoryEntriesPerEndpoint(60); //maximum history of checks
    opt.SetApiMaxActiveRequests(1); //api requests concurrency
    //opt.AddHealthCheckEndpoint("Categories API", $"{settings.CategoriesServiceUri}health"); //map health check api
}).AddInMemoryStorage();

var app = builder.Build();

//app.UseEndpoints(config =>
//{
//    _ = config.MapHealthChecks("/healthz", new HealthCheckOptions
//    {
//        Predicate = _ => true,
//        ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
//    });
//});
app.UseRouting();
app.UseHealthChecks("/health");

//app.MapHealthChecks("/health", new HealthCheckOptions
//{
//    Predicate = _ => true,
//    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
//});

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Application/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

//app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Application}/{action=Welcome}/{id?}");

app.Run();