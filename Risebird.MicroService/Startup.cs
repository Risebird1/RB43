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
using System.IO;
using System.Reflection;

[assembly: FunctionsStartup(typeof(Startup))]
namespace Risebird.MicroService
{
    public class Startup : FunctionsStartup
    {
        public IConfiguration Configuration { get; }
        public override void Configure(IFunctionsHostBuilder builder)
        {
            var currentDirectory = Environment.GetEnvironmentVariable("AzureWebJobsScriptRoot")  // local_root
                   ?? (Environment.GetEnvironmentVariable("HOME") == null
                       ? Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
                       : $"{Environment.GetEnvironmentVariable("HOME")}/site/wwwroot"); // azure_root

            var configuration = new ConfigurationBuilder()
              .SetBasePath(currentDirectory)
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
            builder.Services.AddScoped<ISpeechToTextHelper, SpeechToTextHelper>();
            builder.Services.AddScoped<ICandidateRepository, CandidateRepository>();
            builder.Services.AddScoped<IIRFRepository, IRFRepository>();
            builder.Services.AddScoped<IDashboardService, DashboardService>();
            builder.Services.AddScoped<IDashboarRepository, DashboardRepository>();
            builder.Services.AddScoped<IMeetingRespository, MeetingRespository>();
            builder.Services.AddScoped<IWhatsAppHelper, WhatsAppHelper>();
            builder.Services.AddScoped<IUserService, UserService>();
            builder.Services.AddScoped<IUserRepository, UserRepository>();
            builder.Services.AddScoped<ICalendlyHelper, CalendlyHelper>();
            builder.Services.AddScoped<IZoomHelper, ZoomHelper>();
            builder.Services.AddScoped<ISASUrlHelper, SASUrlHelper>();
            builder.Services.AddScoped<IInterviewerService, InterviewerService>();
            builder.Services.AddScoped<IInterviewerRepository, InterviewerRepository>();
            builder.Services.AddScoped<IClientService, ClientService>();
            builder.Services.AddScoped<IClientRepository, ClientRepository>();

        }


    }
}
