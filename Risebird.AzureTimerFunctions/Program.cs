using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Risebird.AzureFunctions;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();
builder.Services.Configure<Risebird.Entities.Data.ZohoOptions>(builder.Configuration.GetSection("Zoho"));
builder.Services.Configure<Risebird.Entities.Data.ExotelSettings>(builder.Configuration.GetSection("Exotel"));
// Application Insights isn't enabled by default. See https://aka.ms/AAt8mw4.
// builder.Services
//     .AddApplicationInsightsTelemetryWorkerService()
//     .ConfigureFunctionsApplicationInsights();
builder.Services.AddServices();
builder.Build().Run();
