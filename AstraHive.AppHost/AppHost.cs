var builder = DistributedApplication.CreateBuilder(args);

var webApi = builder.AddProject<Projects.AH_WebApi>("ah-webapi");

builder.AddProject<Projects.AH_IdentityService>("ah-identityservice");

builder.AddProject<Projects.AH_SiloHost>("ah-silohost");

builder.AddViteApp("ah-webapp", "../ah.webapp")
    .WithReference(webApi)
    .WaitFor(webApi)
    .WithExternalHttpEndpoints();

builder.Build().Run();
