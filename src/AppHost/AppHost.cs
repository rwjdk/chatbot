using Projects;
using ServiceDefaults;

IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);



IResourceBuilder<ParameterResource> azureOpenAiEndpoint = builder.AddParameter(SecretKeys.azureopenaiendpoint, secret: false);
IResourceBuilder<ParameterResource> azureOpenAiKey = builder.AddParameter(SecretKeys.azureopenaikey, secret: true);

builder.AddProject<ChatBot_BlazorServerOnly>("blazor-server-only")
    .WithEnvironment(SecretKeys.azureopenaiendpoint, azureOpenAiEndpoint)
    .WithEnvironment(SecretKeys.azureopenaikey, azureOpenAiKey);

builder.Build().Run();
