var builder = WebApplication.CreateBuilder(args);

// Routes and destinations live in the "ReverseProxy" configuration section.
// Every new microservice is exposed by adding a route (e.g. /api/inventory/{**catch-all}) and a cluster.
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));
builder.Services.AddHealthChecks();

var app = builder.Build();

app.MapHealthChecks("/health");
app.MapReverseProxy();

await app.RunAsync();
