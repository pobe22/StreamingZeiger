var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.Streamingzeiger_Api>("streamingzeiger-api");

builder.AddProject<Projects.StreamingZeiger_API>("streamingzeiger-api");

builder.Build().Run();
