using Projects;

var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<OmzOmz_WebApi>("api");

builder.Build().Run();