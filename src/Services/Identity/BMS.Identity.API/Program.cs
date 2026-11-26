using BMS.BuildingBlocks.Web;
using BMS.Identity.Application;
using BMS.Identity.Persistence;
using BMS.Identity.Persistence.Seeding;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddIdentityApplication(builder.Configuration);
builder.Services.AddIdentityPersistence(builder.Configuration);
builder.Services.AddHealthChecks().AddDbContextCheck<IdentityDbContext>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    await scope.ServiceProvider.GetRequiredService<IdentityDataSeeder>().SeedAsync();
}

app.UseServiceDefaults();

await app.RunAsync();

public partial class Program;
