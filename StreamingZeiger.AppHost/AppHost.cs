var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.Streamingzeiger_Api>("streamingzeiger-api");

builder.Build().Run();
