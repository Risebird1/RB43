using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Risebird.Business;
using Risebird.Business.Interface;
using Risebird.CommonHelper;
using Risebird.CommonHelper.Interface;
using Risebird.MicroService;
using Risebird.Repository;
using Risebird.Repository.Interface;
using System;

[assembly: FunctionsStartup(typeof(Startup))]
namespace Risebird.MicroService
{
    public class Startup : FunctionsStartup
    {
        public IConfiguration Configuration { get; }
        public override void Configure(IFunctionsHostBuilder builder)
        {
            var configuration = new ConfigurationBuilder()
            .SetBasePath(Environment.CurrentDirectory)
            .AddJsonFile("appsettings.json", true, true)
            .AddEnvironmentVariables()
            .Build();

            builder.Services.AddSingleton<IConfiguration>(configuration);
            builder.Services.AddSingleton<AuthenticationService>();
            builder.Services.AddScoped<IAzureStorageHelper, AzureStorageHelper>();
            builder.Services.AddScoped<IAzureHelper, AzureHelper>();
            builder.Services.AddScoped<IProctorInterviewsRepository, ProctorInterviewsRepository>();
            builder.Services.AddScoped<IProctorInterviewsService, ProctorInterviewsService>();
            builder.Services.AddScoped<INotificationHelper, NotificationHelper>();
            builder.Services.AddScoped<IApplicationErrorLogger, ApplicationErrorLogger>();


        }

        
    }
}
