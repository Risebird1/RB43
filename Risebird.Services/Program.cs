using Risebird.Business;
using Risebird.Business.Interface;
using Risebird.CommonHelper;
using Risebird.CommonHelper.Interface;
using Risebird.Repository;
using Risebird.Repository.Interface;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddControllers(options =>
{
    options.MaxModelBindingCollectionSize = 5000;
}); 

builder.Services.Configure<Risebird.Entities.Data.ZohoOptions>(builder.Configuration.GetSection("Zoho"));



builder.Services.AddSingleton<HttpClientHelper>();
builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
builder.Services.AddSingleton<IConfiguration>(builder.Configuration);
builder.Services.AddScoped<INotificationHelper, NotificationHelper>();
builder.Services.AddScoped<IAzureStorageHelper, AzureStorageHelper>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IProfileService, ProfileService>();
builder.Services.AddScoped<ISkillService, SkillService>();
builder.Services.AddScoped<IClientService, ClientService>();
builder.Services.AddScoped<IMeetingService, MeetingService>();
builder.Services.AddHttpClient<ICashfreePayoutService, CashfreePayoutService>();
builder.Services.AddScoped<IQcInterviewAIMergeJobService, QcInterviewAIMergeJobService>();
builder.Services.AddScoped<IDmIvAutoQcRemovalService, DmIvAutoQcRemovalService>();
builder.Services.AddScoped<IDmIvAutoQcFlagService, DmIvAutoQcFlagService>();
builder.Services.AddScoped<ITransInterviewerAuditRepository, TransInterviewerAuditRepository>();
builder.Services.AddScoped<IAutoQcRemovalMonitorRepository, AutoQcRemovalMonitorRepository>();
builder.Services.AddScoped<IDmIvAutoQcBackfillService, DmIvAutoQcBackfillService>();
builder.Services.AddScoped<IAutoQcRemovalMonitorService, AutoQcRemovalMonitorService>();
builder.Services.AddScoped<IRiskCategoryRepository, RiskCategoryRepository>();
builder.Services.AddScoped<IRiskCategoryService, RiskCategoryService>();
builder.Services.AddScoped<ITrialJudgmentRepository, TrialJudgmentRepository>();
builder.Services.AddScoped<ITrialJudgmentService, TrialJudgmentService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IProfileRepository, ProfileRepository>();
builder.Services.AddScoped<ISkillRepository, SkillRepository>();
builder.Services.AddScoped<IClientRepository, ClientRepository>();
builder.Services.AddScoped<IMeetingRespository, MeetingRespository>();
builder.Services.AddScoped<IApplicationErrorLogger, ApplicationErrorLogger>();
builder.Services.AddScoped<IDashboarRepository, DashboardRepository>();
builder.Services.AddScoped<IIRFRepository, IRFRepository>();
builder.Services.AddRisebirdAzureSearch(builder.Configuration);
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
builder.Services.AddScoped<ISendGridRepository, SendGridRepository>();
builder.Services.AddScoped<IMailRepository, MailRepository>();
builder.Services.AddScoped<IMailService, MailService>();
builder.Services.AddScoped<IWhatsappRepository, WhatsappRepository>();
builder.Services.AddScoped<ISurveyRepository, SurveyRepository>();
builder.Services.AddScoped<ISurveyService, SurveyService>();
builder.Services.AddScoped<ICodeInviteRequest, CodeInviteRequest>();
builder.Services.AddScoped<IValidationHelper, ValidationHelper>();
builder.Services.AddScoped<IPhoneNumberHelper, PhoneNumberHelper>();
builder.Services.AddScoped<IHttpRequestHelper, HttpRequestHelper>();
builder.Services.AddScoped<IAffIndaResumeParserHelper, AffIndaResumeParserHelper>();
builder.Services.AddScoped<IEncryptionHelper, EncryptionHelper>();
builder.Services.AddScoped<IAssistantServiceHelper, AssistantServiceHelper>();
builder.Services.AddScoped<IOpenAIResponsesHelper, OpenAIResponsesHelper>();
builder.Services.AddScoped<IInterviewApiService, InterviewApiService>();
builder.Services.AddScoped<IOpenAIInterviewerAssistantHelper, OpenAIInterviewerAssistantHelper>();
builder.Services.AddScoped<IAIInterviewerRepository, AIInterviewerRepository>();
builder.Services.AddScoped<IOpenAISpeechToTextService, OpenAISpeechToTextService>();
builder.Services.AddScoped<IAIInterviewerService, AIInterviewerService>();
builder.Services.AddScoped<ISynthesiaTextToVideoService, SynthesiaTextToVideoService>();
builder.Services.AddScoped<IZohoMeetingHelper, ZohoMeetingHelper>();
builder.Services.AddScoped<IAWSRekognitionService, AWSRekognitionService>();
builder.Services.AddScoped<IPdfReaderHelper, PdfReaderHelper>();
builder.Services.AddScoped<IOpenAIApiLogRepository, OpenAIApiLogRepository>();
builder.Services.AddScoped<IOpenAIApiLogger, OpenAIApiLogger>();
builder.Services.AddScoped<IProctoringApiRepository, ProctoringApiRepository>();
builder.Services.AddScoped<IProctoringApiService, ProctoringApiService>();
builder.Services.AddSingleton<IProctoringLaunchTokenService, ProctoringLaunchTokenService>();
builder.Services.AddDefaultAWSOptions(builder.Configuration.GetAWSOptions());

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
