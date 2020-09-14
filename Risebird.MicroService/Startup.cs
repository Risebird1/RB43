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


            //DependencyMapping(builder.Services);
        }

        //public static void DependencyMapping(IServiceCollection services)
        //{
        //    services.AddScoped<IAzureHelper, AzureHelper>();

        //}
        //private void DependencyMapping(IServiceCollection services)
        //{
        //    services.AddSingleton<HttpClientHelper>();
        //    services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
        //    services.AddScoped<INotificationHelper, NotificationHelper>();
        //    services.AddScoped<IAzureStorageHelper, AzureStorageHelper>();
        //    services.AddScoped<IUserService, UserService>();
        //    services.AddScoped<IProfileService, ProfileService>();
        //    services.AddScoped<ISkillService, SkillService>();
        //    services.AddScoped<IMenuService, MenuService>();
        //    services.AddScoped<IClientService, ClientService>();
        //    services.AddScoped<IMeetingService, MeetingService>();
        //    services.AddScoped<IDashboardService, DashboardService>();
        //    services.AddScoped<IGeneralSkillMatrixService, GeneralSkillMatrixService>();
        //    services.AddScoped<IJoinabilityMatrixService, JoinabilityMatrixService>();
        //    services.AddScoped<IUserRepository, UserRepository>();
        //    services.AddScoped<IProfileRepository, ProfileRepository>();
        //    services.AddScoped<ISkillRepository, SkillRepository>();
        //    services.AddScoped<IClientRepository, ClientRepository>();
        //    services.AddScoped<IMeetingRespository, MeetingRespository>();
        //    services.AddScoped<IApplicationErrorLogger, ApplicationErrorLogger>();
        //    services.AddScoped<IDashboarRepository, DashboardRepository>();
        //    services.AddScoped<IIRFRepository, IRFRepository>();
        //    services.AddScoped<IJoinabilityMatrixRepository, JoinabilityMatrixRepository>();
        //    services.AddScoped<IGeneralSkillMatrixRepository, GeneralSkillMatrixRepository>();
        //    services.AddScoped<ISelectabilityMatrixRepository, SelectabilityMatrixRepository>();
        //    services.AddScoped<ISelectabilityMatrixService, SelectabilityMatrixService>();
        //    services.AddScoped<IIRFService, IRFService>();
        //    services.AddScoped<ITwilioHelper, TwilioHelper>();
        //    services.AddScoped<IProjectService, ProjectService>();
        //    services.AddScoped<IProjectRepository, ProjectRepository>();
        //    services.AddScoped<IAccountRepository, AccountRepository>();
        //    services.AddScoped<IAccountService, AccountService>();
        //    services.AddScoped<IEvaluatorRepository, EvaluatorRepository>();
        //    services.AddScoped<IEvaluatorService, EvaluatorService>();
        //    services.AddScoped<IVoiceItRepository, VoiceItRepository>();
        //    services.AddScoped<ISupportRepository, SupportRepository>();
        //    services.AddScoped<ISupportService, SupportService>();
        //    services.AddScoped<ISettingsRepository, SettingsRepository>();
        //    services.AddScoped<ISettingsService, SettingsService>();
        //    services.AddScoped<IInterviewerSkillsRepository, InterviewerSkillsRepository>();
        //    services.AddScoped<IInterviewerSkillsService, InterviewerSkillsService>();
        //    services.AddScoped<IInterviewerService, InterviewerService>();
        //    services.AddScoped<IInterviewerRepository, InterviewerRepository>();
        //    services.AddScoped<IAzureFaceApi, AzureFaceApi>();
        //    services.AddScoped<IChatService, ChatService>();
        //    services.AddScoped<IChatRepository, ChatRepository>();
        //    services.AddScoped<IMeetingInviteService, MeetingInviteService>();
        //    services.AddScoped<IMeetingInviteRepository, MeetingInviteRepository>();
        //    services.AddScoped<IProctorInterviewsService, ProctorInterviewsService>();
        //    services.AddScoped<IProctorInterviewsRepository, ProctorInterviewsRepository>();
        //    services.AddScoped<IAzureHelper, AzureHelper>();

        //}
    }
}
