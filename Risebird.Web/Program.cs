using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Identity.Web;
using Risebird.Business;
using Risebird.Business.Interface;
using Risebird.CommonHelper;
using Risebird.CommonHelper.Interface;
using Risebird.Entities;
using Risebird.Repository;
using Risebird.Repository.Interface;
using Risebird.Web.Extensions;
using System;
using System.IO.Compression;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews().AddRazorRuntimeCompilation();
builder.Services.AddMicrosoftIdentityWebAppAuthentication(builder.Configuration, "Authentication:AzureAdB2C");
builder.Services.Configure<ConfigurationOptions>(options => builder.Configuration.Bind(options));

var configOptions = new ConfigurationOptions();
builder.Configuration.Bind(configOptions);

builder.Services.Configure<FormOptions>(x =>
{
    x.ValueCountLimit = int.MaxValue;
    x.ValueLengthLimit = int.MaxValue;
    x.MultipartBodyLengthLimit = int.MaxValue; // In case of multipart
});

builder.Services.Configure<BrotliCompressionProviderOptions>(options =>
{
    options.Level = CompressionLevel.Optimal;
});

builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true; options.Providers.Add<BrotliCompressionProvider>();
});

builder.Services.AddCors(o => o.AddPolicy("MyPolicy", builder =>
{
    builder.AllowAnyOrigin()
           .AllowAnyMethod()
           .AllowAnyHeader();
}));

builder.Services.Configure<FormOptions>(o =>
{
    o.ValueLengthLimit = int.MaxValue;
    o.MultipartBodyLengthLimit = int.MaxValue;
    o.MemoryBufferThreshold = int.MaxValue;
});
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(90);
    options.Cookie.Name = "Risebirdportal.Session";
    //options.Cookie.HttpOnly = true;
});

//Configure session state
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.InstanceName = "risebirdportalui";
    options.Configuration = "risebirdportal.redis.cache.windows.net:6380,password=XIsWSLMvXVfhiFbm94eKPfcm2wSlHgIGtAzCaFuEFU4=,ssl=True,abortConnect=False";
});
//builder.Services.AddDistributedRedisCache(options =>
//{
//    options.InstanceName = "risebirdportalui";
//    options.Configuration = "risebirdportalnew.redis.cache.windows.net:6380,password=63HxF9rAS5oopAdrfqPt5oFQqoTiklD2lAzCaKXgquw=,ssl=True,abortConnect=False";
//});

//builder.Services.AddMvc();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddApplicationInsightsTelemetry();

builder.Services.AddHsts(options =>
{
    options.IncludeSubDomains = true;
    options.MaxAge = TimeSpan.FromSeconds(31536000);
});


builder.Services.AddSingleton<HttpClientHelper>();
builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
builder.Services.AddSingleton<IConfiguration>(builder.Configuration);
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


var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseBrowserLink();
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Index");
    app.UseHsts();
}
// Configure the HTTP request pipeline.

app.UseCsp(builder =>
{
    builder.Defaults
           .AllowSelf()
           .Allow("wss://chunderw-gll.twilio.com/signal")
           .Allow("wss://matrix.twilio.com")
           .Allow("https://wchat.freshchat.com/")
           .Allow("blob:")
           .Allow("https://www.google-analytics.com/")
           .Allow("https://dc.services.visualstudio.com/")
           .Allow("https://centralindia-0.in.applicationinsights.azure.com//v2/track")
           .Allow("https://cdn.tiny.cloud/1/no-api-key/")
           .Allow("https://cdnjs.com/libraries/material-components-web/")
           .Allow("https://fw-cdn.com/")
           .Allow("https://risebird-org-75be704f472209616727480.freshchat.com/")
           .Allow("https://widget.freshworks.com/")
           .Allow("'unsafe-inline'");

    builder.Scripts
           .AllowSelf()
           .Allow("https://risebirdblob.blob.core.windows.net/")
           .Allow("https://unpkg.com/")
           .Allow("https://code.jquery.com/")
           .Allow("https://static.twilio.com/")
           .Allow("https://cdnjs.cloudflare.com/")
           .Allow("https://cdnjs.cloudflare.com/")
           .Allow("https://html2canvas.hertzen.com/")
           .Allow("https://ajax.googleapis.com/")
           .Allow("https://code.highcharts.com/")
           .Allow("https://maxcdn.bootstrapcdn.com/")
           .Allow("https://canvasjs.com/")
           .Allow("https://cdn.jsdelivr.net/")
           .Allow("https://cdn.datatables.net/")
           .Allow("https://static.twilio.com/")
           .Allow("https://www.googletagmanager.com/")
           .Allow("https://www.google-analytics.com/")
           //.Allow("https://prodvideo.blob.core.windows.net/")
           .Allow("https://amp.azure.net/")
           .Allow("https://assets.calendly.com/")
           .Allow("https://www.amcharts.com/")
           .Allow("https://wchat.freshchat.com/")
           .Allow("https://az416426.vo.msecnd.net")
           .Allow("https://cdn.rawgit.com/")
           .Allow("https://dc.services.visualstudio.com/")
           .Allow("https://js.monitor.azure.com/scripts/b/ai.2.min.js")
           .Allow("https://centralindia-0.in.applicationinsights.azure.com//v2/track")
           //.Allow("https://cdn.tiny.cloud/1/no-api-key/tinymce/6/tinymce.min.js")
           .Allow("https://cdn.tiny.cloud/1/no-api-key/")
           .Allow("https://cdnjs.com/libraries/material-components-web/")
           .Allow("https://fw-cdn.com/")
           .Allow("https://widget.freshworks.com/")
           .Allow("https://risebird-org-75be704f472209616727480.freshchat.com/")
           .Allow("https://sdk.twilio.com/")
           .Allow("blob:")
           .Allow("'unsafe-eval'")
           .Allow("'unsafe-inline'");

    builder.Styles
           .AllowSelf()
           .Allow("https://risebirdblob.blob.core.windows.net/")
           .Allow("https://fonts.googleapis.com/")
           .Allow("https://use.fontawesome.com/")
           .Allow("https://cdn.jsdelivr.net/")
           .Allow("https://unpkg.com/")
           .Allow("https://cdnjs.cloudflare.com/")
           .Allow("https://maxcdn.bootstrapcdn.com/")
           .Allow("https://pro.fontawesome.com/")
           .Allow("https://static.twilio.com/")
           .Allow("https://amp.azure.net/")
           .Allow("https://www.amcharts.com/")
           .Allow("https://wchat.freshchat.com/")
           .Allow("https://cdn.tiny.cloud/1/no-api-key/")
           .Allow("https://cdnjs.com/libraries/material-components-web/")
           .Allow("https://widget.freshworks.com/")
           .Allow("https://risebird-org-75be704f472209616727480.freshchat.com/")
           .Allow("https://fw-cdn.com/")
           //.Allow("https://widget.freshworks.com/widgetBase/static/media/frame.d7ae132c.css")
           .Allow("'unsafe-inline'");

    builder.Fonts
           .AllowSelf()
           .Allow("https://risebirdblob.blob.core.windows.net/")
           .Allow("https://fonts.gstatic.com/")
           .Allow("https://use.fontawesome.com/")
           .Allow("https://pro.fontawesome.com/")
           .Allow("https://maxcdn.bootstrapcdn.com/")
           .Allow("https://static.twilio.com/")
           .Allow("https://amp.azure.net/")
           .Allow("https://www.amcharts.com/")
           .Allow("https://wchat.freshchat.com/")
           .Allow("https://cdnjs.cloudflare.com/")
           .Allow("https://cdn.tiny.cloud/1/no-api-key/")
           .Allow("https://cdnjs.com/libraries/material-components-web/")
           .Allow("https://widget.freshworks.com/")

           .Allow("'unsafe-inline'");

    builder.Images
           .AllowSelf()
           .Allow("https://risebirdblob.blob.core.windows.net/")
           .Allow("https://risebirdportal.blob.core.windows.net/")
           .Allow("https://icla.org/")
           .Allow("https://static.twilio.com/")
           .Allow("https://static.tumblr.com/")
           .Allow("http://www.w3.org/")
           .Allow("https://www.google-analytics.com/")
           .Allow("https://cdnjs.cloudflare.com/ajax/libs/intl-tel-input/")
           .Allow("https://d3v0px0pttie1i.cloudfront.net/")
           .Allow("https://sp.tinymce.com/")
           .Allow("https://cdn.tiny.cloud/1/no-api-key/")
           .Allow("https://cdnjs.com/libraries/material-components-web/")
           .Allow("https://imgur.com/")
           .Allow("https://widget.freshworks.com/")
           .Allow("data:")
           .Allow("'unsafe-inline'");

    builder.Media
           .AllowSelf()
           .Allow("https://risebirdportal.blob.core.windows.net/")
           .Allow("https://prodvideo.blob.core.windows.net/")
           .Allow("https://static.twilio.com/")
           .Allow("https://api.twilio.com/")
           .Allow("https://risebirdvideos.blob.core.windows.net/")
           .Allow("https://meet.risebird.io/server/recordings/")
           .Allow("https://cdn.tiny.cloud/1/no-api-key/")
           .Allow("https://cdnjs.com/libraries/material-components-web/")
           .Allow("https://widget.freshworks.com/")
           .Allow("https://portal.risebird.io/");

    builder.Frame
           .AllowSelf()
           .Allow("https://wchat.freshchat.com/")
           .Allow("https://calendly.com/")
           .Allow("https://cdn.tiny.cloud/1/no-api-key/")
           .Allow("https://app.shuftipro.com/")
           .Allow("https://cdnjs.com/libraries/material-components-web/")
           .Allow("https://widget.freshworks.com/")
           .Allow("https://risebird-org-75be704f472209616727480.freshchat.com/")
           .Allow("https://fw-cdn.com/")
           //.Allow("https://portal.risebird.io/")
           .Allow("*.webpush.freshchat.com/")
           .Allow("data:");
    //builder.Object
    //       //.AllowSelf()
    //       .Allow("data:")
    //       .Allow("'unsafe-eval'");

});

app.Use(async (context, next) =>
{
    context.Response.Headers.Add("X-Xss-Protection", "1; mode=block");
    context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
    await next();
});

app.UseSession();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseCors(options => options.AllowAnyOrigin());
app.UseResponseCompression();

app.UseHttpsRedirection();

//app.UseStatusCodePagesWithReExecute("/Index");
app.UseStaticFiles();
//app.UseRouting();
//app.UseEndpoints(opts =>
//{
//    opts.MapControllers();
//});
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

//app.UseMiddleware<ValidateAntiForgeryTokenMiddleware>();

app.Run();

