using Microsoft.ApplicationInsights.Extensibility;
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

            builder.Services.AddSingleton<HttpClientHelper>();
            //builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            builder.Services.AddScoped<INotificationHelper, NotificationHelper>();
            builder.Services.AddScoped<IAzureStorageHelper, AzureStorageHelper>();
            builder.Services.AddScoped<IUserService, UserService>();
            builder.Services.AddScoped<IProfileService, ProfileService>();
            builder.Services.AddScoped<ISkillService, SkillService>();
            builder.Services.AddScoped<IMenuService, MenuService>();
            builder.Services.AddScoped<IClientService, ClientService>();
            builder.Services.AddScoped<IMeetingService, MeetingService>();
            builder.Services.AddScoped<IDashboardService, DashboardService>();
            builder.Services.AddScoped<IGeneralSkillMatrixService, GeneralSkillMatrixService>();
            builder.Services.AddScoped<IJoinabilityMatrixService, JoinabilityMatrixService>();
            builder.Services.AddScoped<IUserRepository, UserRepository>();
            builder.Services.AddScoped<IProfileRepository, ProfileRepository>();
            builder.Services.AddScoped<ISkillRepository, SkillRepository>();
            builder.Services.AddScoped<IClientRepository, ClientRepository>();
            builder.Services.AddScoped<IMeetingRespository, MeetingRespository>();
            builder.Services.AddScoped<IApplicationErrorLogger, ApplicationErrorLogger>();
            builder.Services.AddScoped<IDashboarRepository, DashboardRepository>();
            builder.Services.AddScoped<IIRFRepository, IRFRepository>();
            builder.Services.AddScoped<IJoinabilityMatrixRepository, JoinabilityMatrixRepository>();
            builder.Services.AddScoped<IGeneralSkillMatrixRepository, GeneralSkillMatrixRepository>();
            builder.Services.AddScoped<ISelectabilityMatrixRepository, SelectabilityMatrixRepository>();
            builder.Services.AddScoped<ISelectabilityMatrixService, SelectabilityMatrixService>();
            builder.Services.AddScoped<IIRFService, IRFService>();
            builder.Services.AddScoped<ITwilioHelper, TwilioHelper>();
            builder.Services.AddScoped<IProjectService, ProjectService>();
            builder.Services.AddScoped<IProjectRepository, ProjectRepository>();
            builder.Services.AddScoped<IAccountRepository, AccountRepository>();
            builder.Services.AddScoped<IAccountService, AccountService>();
            builder.Services.AddScoped<IEvaluatorRepository, EvaluatorRepository>();
            builder.Services.AddScoped<IEvaluatorService, EvaluatorService>();
            builder.Services.AddScoped<IVoiceItRepository, VoiceItRepository>();
            builder.Services.AddScoped<ISupportRepository, SupportRepository>();
            builder.Services.AddScoped<ISupportService, SupportService>();
            builder.Services.AddScoped<ISettingsRepository, SettingsRepository>();
            builder.Services.AddScoped<ISettingsService, SettingsService>();
            builder.Services.AddScoped<IInterviewerSkillsRepository, InterviewerSkillsRepository>();
            builder.Services.AddScoped<IInterviewerSkillsService, InterviewerSkillsService>();
            builder.Services.AddScoped<IInterviewerService, InterviewerService>();
            builder.Services.AddScoped<IInterviewerRepository, InterviewerRepository>();
            builder.Services.AddScoped<IAzureFaceApi, AzureFaceApi>();
            builder.Services.AddScoped<IChatService, ChatService>();
            builder.Services.AddScoped<IChatRepository, ChatRepository>();
            builder.Services.AddScoped<IMeetingInviteService, MeetingInviteService>();
            builder.Services.AddScoped<IMeetingInviteRepository, MeetingInviteRepository>();
            builder.Services.AddScoped<IProctorInterviewsService, ProctorInterviewsService>();
            builder.Services.AddScoped<IProctorInterviewsRepository, ProctorInterviewsRepository>();
            builder.Services.AddScoped<IAzureHelper, AzureHelper>();
            builder.Services.AddScoped<IRisebirdPayService, RisebirdPayService>();
            builder.Services.AddScoped<IRisebirdPayRepository, RisebirdPayRepository>();
            builder.Services.AddScoped<ICandidateRepository, CandidateRepository>();
            builder.Services.AddScoped<ICandidateService, CandidateService>();
            builder.Services.AddScoped<ISpeechToTextHelper, SpeechToTextHelper>();
            builder.Services.AddScoped<IActivityTrackerRepository, ActivityTrackerRepository>();
            builder.Services.AddScoped<IActivityTrackerService, ActivityTrackerService>();
            builder.Services.AddScoped<IUnapprovedInterviewerMeetingsService, UnapprovedInterviewerMeetingsService>();
            builder.Services.AddScoped<IUnapprovedInterviewerMeetingsRepository, UnapprovedInterviewerMeetingsRepository>();
            builder.Services.AddScoped<IB2CClientService, B2CClientService>();
            builder.Services.AddScoped<IPasswordHelper, PasswordHelper>();
            builder.Services.AddScoped<IWhatsAppHelper, WhatsAppHelper>();
            builder.Services.AddScoped<ISASUrlHelper, SASUrlHelper>();
            builder.Services.AddScoped<ITemplateService, TemplateService>();
            builder.Services.AddScoped<ITemplateRepository, TemplateRepository>();
            builder.Services.AddScoped<IInstaInterviewService, InstaInterviewService>();
            builder.Services.AddScoped<IInstaInterviewRepository, InstaInterviewRepository>();
            builder.Services.AddScoped<ICalendlyHelper, CalendlyHelper>();
            builder.Services.AddScoped<IFileMimeTypeHelper, FileMimeTypeHelper>();
            builder.Services.AddScoped<IInterviewRequestService, InterviewRequestService>();
            builder.Services.AddScoped<IInterviewRequestRepository, InterviewRequestRepository>();
            builder.Services.AddScoped<IInterviewSchedulerService, InterviewSchedulerService>();
            builder.Services.AddScoped<IInterviewSchedulerRepository, InterviewSchedulerRepository>();
            builder.Services.AddScoped<IRisebirdMeetingsRepository, RisebirdMeetingsRepository>();
            builder.Services.AddScoped<IZoomHelper, ZoomHelper>();
            builder.Services.AddScoped<IRecommendedIRFService, RecommendedIRFService>();
            builder.Services.AddScoped<IRecommendedIRFRepository, RecommendedIRFRepository>();
            builder.Services.AddScoped<IClonableSkillChannelTemplateService, ClonableSkillChannelTemplateService>();
            builder.Services.AddScoped<IClonableSkillChannelTemplateRepository, ClonableSkillChannelTemplateRepository>();
            builder.Services.AddScoped<IMCQService, MCQService>();
            builder.Services.AddScoped<IMCQRepository, MCQRepository>();
            builder.Services.AddScoped<IRBMeetHelper, RBMeetHelper>();
            builder.Services.AddScoped<IAadhaarService, AadhaarService>();
            builder.Services.AddScoped<IAadhaarRepository, AadhaarRepository>();
            builder.Services.AddSingleton<ITelemetryInitializer, TelemetryInitializer>();
            builder.Services.AddSingleton<ITrackPublicUrlAccessor, TrackPublicUrlAccessor>();
            builder.Services.AddScoped<ISendGridRepository, SendGridRepository>();
            builder.Services.AddScoped<IMailRepository, MailRepository>();
            builder.Services.AddScoped<IMailService, MailService>();
            builder.Services.AddScoped<IWhatsappRepository, WhatsappRepository>();
            builder.Services.AddScoped<ISurveyRepository, SurveyRepository>();
            builder.Services.AddScoped<ISurveyService, SurveyService>();
            builder.Services.AddScoped<ICodeInviteRequest, CodeInviteRequest>();
            builder.Services.AddScoped<IValidationHelper, ValidationHelper>();
            builder.Services.AddScoped<IHttpRequestHelper, HttpRequestHelper>();
            builder.Services.AddScoped<IVmHelper, VmHelper>();
            builder.Services.AddScoped<IAffIndaResumeParserHelper, AffIndaResumeParserHelper>();
            builder.Services.AddScoped<IEncryptionHelper, EncryptionHelper>();
        }


    }
}
