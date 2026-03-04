var builder = DistributedApplication.CreateBuilder(args);

var db = builder.AddPostgres("postgres")
    .AddDatabase("ChairlyDb");

builder.AddProject<Projects.Chairly_Api>("api")
    .WithReference(db)
    .WaitFor(db)
    .WithUrlForEndpoint("http", ep => new ResourceUrlAnnotation { Url = "/scalar", DisplayText = "Scalar" });

builder.Build().Run();
