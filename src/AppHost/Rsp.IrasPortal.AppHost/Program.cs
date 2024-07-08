using Microsoft.Extensions.Configuration;
using Rsp.IrasPortal.AppHost.Configuration;

var builder = DistributedApplication.CreateBuilder(args);

var configuration = builder.Configuration;

// get the configuration settings for the projects
var projectSettingsSection = configuration.GetSection(nameof(ProjectsSettings));
var projectSettings = projectSettingsSection.Get<ProjectsSettings>();

// get the configuration settings for iras service
var irasServiceSettings = projectSettings.IrasServiceSettings;
var usersServiceSettings = projectSettings.UsersServiceSettings;

// add the project reference
var irasService = builder.AddProject(irasServiceSettings.ProjectName, irasServiceSettings.ProjectPath);
var usersService = builder.AddProject(usersServiceSettings.ProjectName, usersServiceSettings.ProjectPath);

// add the portal project with a dependency on
// irasService
builder
    .AddProject<Projects.Rsp_IrasPortal>("iras-portal")
    .WithReference(irasService)
    .WithReference(usersService);

// run the host app
await builder.Build().RunAsync();