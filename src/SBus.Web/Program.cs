using SBus.Infrastructure.Data;
using SBus.Web;
using SBus.Web.Api;
using SBus.Web.Api.Errors;

using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddPresentation(builder.Configuration)
    .AddApplication()
    .AddInfrastructure(builder.Configuration)
    .AddMobileApi(builder.Configuration);

builder.Host.UseSerilog((context, loggerConfig) =>
    loggerConfig.ReadFrom.Configuration(context.Configuration));

var app = builder.Build();

if (app.Environment.IsDevelopment() && builder.Configuration.GetValue("AppSettings:InitializeDatabaseOnStartup", false))
{
    await app.InitialiseDatabaseAsync();
}

app.UseMobileApiErrors();

if (!app.Environment.IsDevelopment())
{
    app.UseWhen(context => !ApiProblems.IsApi(context), branch => branch.UseExceptionHandler("/Error"));
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseCoreMiddlewares();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger(options => options.RouteTemplate = "api/docs/{documentName}/swagger.json");
    app.UseSwaggerUI(options =>
    {
        options.RoutePrefix = "api/docs";
        options.SwaggerEndpoint("/api/docs/v1/swagger.json", "S Bus v1");
    });
}

app.MapControllers().RequireCors(ApiRegistration.CorsPolicy);
app.MapFallback("/api/{**path}", ApiFallback.HandleAsync);

app.MapStaticAssets();

app.MapRazorPages()
   .WithStaticAssets();

app.Run();

public partial class Program;