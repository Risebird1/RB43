using DinkToPdf;
using DinkToPdf.Contracts;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Risebird.Business;
using Risebird.Business.Interface;
using Risebird.CommonHelper;
using Risebird.CommonHelper.Interface;
using Risebird.Repository;
using Risebird.Repository.Interface;
using System;

namespace Risebird.MicroService
{
    public class Program
    {
        public static void Main()
        {
            var host = new HostBuilder()
                .ConfigureFunctionsWorkerDefaults()
                .ConfigureServices(services =>
                {
                    var serviceProvider = services.BuildServiceProvider();
                    string currentDirectory = string.Empty;
                    // if running in azure  
                    if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WEBSITE_INSTANCE_ID")))
                    {
                        currentDirectory = Environment.GetEnvironmentVariable("HOME") + @"\site\wwwroot";
                    }
                    // running in non azure environments 
                    else
                    {
                        currentDirectory = Environment.GetEnvironmentVariable("AzureWebJobsScriptRoot");
                    }


                    var configurationBuilder = new ConfigurationBuilder();


                    #region Add the existing config including host.json to builder
                    var existingConfigInstance = serviceProvider.GetService<IConfiguration>();
                    if (existingConfigInstance != null)
                    {
                        configurationBuilder.AddConfiguration(existingConfigInstance);
                    }
                    #endregion
                    var configuration = new ConfigurationBuilder()
                     .SetBasePath(currentDirectory)
                     .AddJsonFile("appsettings.json", true, true)
                     .AddJsonFile("local.settings.json", true, true)
                     .AddEnvironmentVariables()
                     .AddCommandLine(Environment.GetCommandLineArgs())
                     .Build();
                    services.AddSingleton<IConfiguration>(configuration);

                    services.AddSingleton<AuthenticationService>();

                    services.AddSingleton<HttpClientHelper>();
                    services.AddSingleton(typeof(IConverter), new SynchronizedConverter(new PdfTools()));
                    services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
                    services.AddScoped<INotificationHelper, NotificationHelper>();
                    services.AddScoped<IAzureStorageHelper, AzureStorageHelper>();
                    services.AddScoped<IUserService, UserService>();
                    services.AddScoped<IProfileService, ProfileService>();
                    services.AddScoped<ISkillService, SkillService>();
                    services.AddScoped<IMenuService, MenuService>();
                    services.AddScoped<IClientService, ClientService>();
                    services.AddScoped<IMeetingService, MeetingService>();
                    services.AddScoped<IDashboardService, DashboardService>();
                    services.AddScoped<IGeneralSkillMatrixService, GeneralSkillMatrixService>();
                    services.AddScoped<IJoinabilityMatrixService, JoinabilityMatrixService>();
                    services.AddScoped<IUserRepository, UserRepository>();
                    services.AddScoped<IProfileRepository, ProfileRepository>();
                    services.AddScoped<ISkillRepository, SkillRepository>();
                    services.AddScoped<IClientRepository, ClientRepository>();
                    services.AddScoped<IMeetingRespository, MeetingRespository>();
                    services.AddScoped<IApplicationErrorLogger, ApplicationErrorLogger>();
                    services.AddScoped<IDashboarRepository, DashboardRepository>();
                    services.AddScoped<IIRFRepository, IRFRepository>();
                    services.AddScoped<IJoinabilityMatrixRepository, JoinabilityMatrixRepository>();
                    services.AddScoped<IGeneralSkillMatrixRepository, GeneralSkillMatrixRepository>();
                    services.AddScoped<ISelectabilityMatrixRepository, SelectabilityMatrixRepository>();
                    services.AddScoped<ISelectabilityMatrixService, SelectabilityMatrixService>();
                    services.AddScoped<IIRFService, IRFService>();
                    services.AddScoped<ITwilioHelper, TwilioHelper>();
                    services.AddScoped<IProjectService, ProjectService>();
                    services.AddScoped<IProjectRepository, ProjectRepository>();
                    services.AddScoped<IAccountRepository, AccountRepository>();
                    services.AddScoped<IAccountService, AccountService>();
                    services.AddScoped<IEvaluatorRepository, EvaluatorRepository>();
                    services.AddScoped<IEvaluatorService, EvaluatorService>();
                    services.AddScoped<IVoiceItRepository, VoiceItRepository>();
                    services.AddScoped<ISupportRepository, SupportRepository>();
                    services.AddScoped<ISupportService, SupportService>();
                    services.AddScoped<ISettingsRepository, SettingsRepository>();
                    services.AddScoped<ISettingsService, SettingsService>();
                    services.AddScoped<IInterviewerSkillsRepository, InterviewerSkillsRepository>();
                    services.AddScoped<IInterviewerSkillsService, InterviewerSkillsService>();
                    services.AddScoped<IInterviewerService, InterviewerService>();
                    services.AddScoped<IInterviewerRepository, InterviewerRepository>();
                    services.AddScoped<IAzureFaceApi, AzureFaceApi>();
                    services.AddScoped<IChatService, ChatService>();
                    services.AddScoped<IChatRepository, ChatRepository>();
                    services.AddScoped<IMeetingInviteService, MeetingInviteService>();
                    services.AddScoped<IMeetingInviteRepository, MeetingInviteRepository>();
                    services.AddScoped<IProctorInterviewsService, ProctorInterviewsService>();
                    services.AddScoped<IProctorInterviewsRepository, ProctorInterviewsRepository>();
                    services.AddScoped<IAzureHelper, AzureHelper>();
                    services.AddScoped<IRisebirdPayService, RisebirdPayService>();
                    services.AddScoped<IRisebirdPayRepository, RisebirdPayRepository>();
                    services.AddScoped<ICandidateRepository, CandidateRepository>();
                    services.AddScoped<ICandidateService, CandidateService>();
                    services.AddScoped<ISpeechToTextHelper, SpeechToTextHelper>();
                    services.AddScoped<IActivityTrackerRepository, ActivityTrackerRepository>();
                    services.AddScoped<IActivityTrackerService, ActivityTrackerService>();
                    services.AddScoped<IUnapprovedInterviewerMeetingsService, UnapprovedInterviewerMeetingsService>();
                    services.AddScoped<IUnapprovedInterviewerMeetingsRepository, UnapprovedInterviewerMeetingsRepository>();
                    services.AddScoped<IB2CClientService, B2CClientService>();
                    services.AddScoped<IPasswordHelper, PasswordHelper>();
                    services.AddScoped<IWhatsAppHelper, WhatsAppHelper>();
                    services.AddScoped<ISASUrlHelper, SASUrlHelper>();
                    services.AddScoped<ITemplateService, TemplateService>();
                    services.AddScoped<ITemplateRepository, TemplateRepository>();
                    services.AddScoped<IInstaInterviewService, InstaInterviewService>();
                    services.AddScoped<IInstaInterviewRepository, InstaInterviewRepository>();
                    services.AddScoped<ICalendlyHelper, CalendlyHelper>();
                    services.AddScoped<IFileMimeTypeHelper, FileMimeTypeHelper>();
                    services.AddScoped<IInterviewRequestService, InterviewRequestService>();
                    services.AddScoped<IInterviewRequestRepository, InterviewRequestRepository>();
                    services.AddScoped<IInterviewSchedulerService, InterviewSchedulerService>();
                    services.AddScoped<IInterviewSchedulerRepository, InterviewSchedulerRepository>();
                    services.AddScoped<IRisebirdMeetingsRepository, RisebirdMeetingsRepository>();
                    services.AddScoped<IZoomHelper, ZoomHelper>();
                    services.AddScoped<IRecommendedIRFService, RecommendedIRFService>();
                    services.AddScoped<IRecommendedIRFRepository, RecommendedIRFRepository>();
                    services.AddScoped<IClonableSkillChannelTemplateService, ClonableSkillChannelTemplateService>();
                    services.AddScoped<IClonableSkillChannelTemplateRepository, ClonableSkillChannelTemplateRepository>();
                    services.AddScoped<IMCQService, MCQService>();
                    services.AddScoped<IMCQRepository, MCQRepository>();
                    services.AddScoped<IRBMeetHelper, RBMeetHelper>();
                    services.AddScoped<IAadhaarService, AadhaarService>();
                    services.AddScoped<IAadhaarRepository, AadhaarRepository>();
                    services.AddSingleton<ITelemetryInitializer, TelemetryInitializer>();
                    services.AddSingleton<ITrackPublicUrlAccessor, TrackPublicUrlAccessor>();
                    services.AddScoped<ISendGridRepository, SendGridRepository>();
                    services.AddScoped<IMailRepository, MailRepository>();
                    services.AddScoped<IMailService, MailService>();
                    services.AddScoped<IWhatsappRepository, WhatsappRepository>();
                    services.AddScoped<ISurveyRepository, SurveyRepository>();
                    services.AddScoped<ISurveyService, SurveyService>();
                    services.AddScoped<ICodeInviteRequest, CodeInviteRequest>();
                    services.AddScoped<IValidationHelper, ValidationHelper>();
                    services.AddScoped<IHttpRequestHelper, HttpRequestHelper>();
                    services.AddScoped<IVmHelper, VmHelper>();
                    services.AddScoped<IGoogleEventHelper, GoogleEventHelper>();
                    services.AddScoped<IAffIndaResumeParserHelper, AffIndaResumeParserHelper>();
                    services.AddScoped<IEncryptionHelper, EncryptionHelper>();
                    services.AddScoped<IAzureStorageRepository, AzureStorageRepository>();
                    services.AddScoped<IAzureStorageService, AzureStorageService>();
                    services.AddScoped<IExcelHelper, ExcelHelper>();
                    services.AddScoped<IAssistantServiceHelper, AssistantServiceHelper>();
                    services.AddScoped<ITeamsMeetingHelper, TeamsMeetingHelper>();
                    services.AddScoped<IInterviewApiService, InterviewApiService>();
                    services.AddScoped<IOpenAIInterviewerAssistantHelper, OpenAIInterviewerAssistantHelper>();
                    services.AddScoped<IAIInterviewerRepository, AIInterviewerRepository>();
                    services.AddScoped<IOpenAISpeechToTextService, OpenAISpeechToTextService>();
                    services.AddScoped<IAIInterviewerService, AIInterviewerService>();
                }).Build();
            host.Run();
        }
    }
}
