using SBus.Infrastructure.Data;
using SBus.Web;

using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddPresentation(builder.Configuration)
    .AddApplication()
    .AddInfrastructure(builder.Configuration);

builder.Host.UseSerilog((context, loggerConfig) =>
    loggerConfig.ReadFrom.Configuration(context.Configuration));

var app = builder.Build();

if (app.Environment.IsDevelopment() && builder.Configuration.GetValue("AppSettings:InitializeDatabaseOnStartup", false))
{
    await app.InitialiseDatabaseAsync();
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseCoreMiddlewares();

app.MapStaticAssets();

app.MapRazorPages()
   .WithStaticAssets();

app.Run();

public partial class Program;
