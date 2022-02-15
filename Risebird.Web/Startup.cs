using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.CookiePolicy;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Risebird.Business;
using Risebird.Business.Interface;
using Risebird.CommonHelper;
using Risebird.CommonHelper.Interface;
using Risebird.Entities;
using Risebird.Repository;
using Risebird.Repository.Interface;
using Risebird.Web.Extensions;
using Risebird.Web.Hepler;
using System;
using System.IO.Compression;

namespace Risebird.Web
{
    public partial class Startup
    {
        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();
            Configuration = builder.Build();
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<ConfigurationOptions>(options => Configuration.Bind(options));

            var configOptions = new ConfigurationOptions();
            Configuration.Bind(configOptions);
            services.Configure<FormOptions>(x =>
            {
                x.ValueCountLimit = int.MaxValue;
                x.ValueLengthLimit = int.MaxValue;
                x.MultipartBodyLengthLimit = int.MaxValue; // In case of multipart
            });
            //Configure session state
            services.AddDistributedRedisCache(options =>
            {
                options.InstanceName = "risebirdportalui";
                options.Configuration = "risebirdportal2.redis.cache.windows.net:6380,password=D61Xuo4tWZvWAckfOS4dRG+EF8WrmmY+XlxfTDzxZAA=,ssl=True,abortConnect=False";
            });
            services.AddAuthentication(sharedOptions =>
                {
                    sharedOptions.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                    sharedOptions.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
                })
                .AddAzureAdB2C(options => Configuration.Bind("Authentication:AzureAdB2C", options))
                .AddCookie();
          


            services.Configure<BrotliCompressionProviderOptions>(options =>
            {
                options.Level = CompressionLevel.Optimal;
            });
            services.AddResponseCompression(options =>
            {
                options.EnableForHttps = true; options.Providers.Add<BrotliCompressionProvider>();
            });
            // Add Cors
            services.AddCors(o => o.AddPolicy("MyPolicy", builder =>
            {
                builder.AllowAnyOrigin()
                       .AllowAnyMethod()
                       .AllowAnyHeader();
            }));
            services.AddMvc();
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddSingleton<IConfiguration>(Configuration);
            services.AddDistributedMemoryCache();
            services.AddSession(options =>
            {               
                options.IdleTimeout = TimeSpan.FromMinutes(90);
                options.Cookie.Name = "Risebirdportal.Session";
                //options.Cookie.HttpOnly = true;
            });
            //Set this values according to your requirements  
            services.Configure<FormOptions>(o =>
            {
                o.ValueLengthLimit = int.MaxValue;
                o.MultipartBodyLengthLimit = int.MaxValue;
                o.MemoryBufferThreshold = int.MaxValue;
            });

            services.AddApplicationInsightsTelemetry();
            //Dependency injection
            DependencyMapping(services);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseBrowserLink();
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Index");
            }
            //app.Use(async (context, next) =>
            //{
            //    //context.Response.Headers.Add("Content-Security-Policy", "default-src 'self';");
            //    //context.Response.Headers.Add("Content-Security-Policy-Report-Only", "default-src 'self'");
            //    context.Response.Headers.Add(
            //    "Content-Security-Policy-Report-Only",
            //    "script-src 'self''unsafe-line''unsafe-inline'; " +
            //    "script-src-elem 'self''unsafe-inline'; " +
            //    "style-src 'self''unsafe-line''unsafe-inline'; " +
            //    "style-src-elem 'self''unsafe-line''unsafe-inline'; " +
            //    "img-src 'self'");

            //    await next();
            //});
            app.UseCsp(builder =>
            {
                builder.Defaults
                       .AllowSelf()
                       .Allow("wss://chunderw-gll.twilio.com/signal")
                       .Allow("wss://matrix.twilio.com")
                       .Allow("https://wchat.freshchat.com/")
                       .Allow("blob:")
                       .Allow("https://www.google-analytics.com/");

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
                       .Allow("blob:")
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
                       .Allow("data:")
                       //.Allow("data:image/svg+xml,%3csvg")
                       .Allow("'unsafe-inline'");

                builder.Media
                       .AllowSelf()
                       .Allow("https://risebirdportal.blob.core.windows.net/")
                       .Allow("https://prodvideo.blob.core.windows.net/")
                       .Allow("https://static.twilio.com/");

                builder.Frame
                       .AllowSelf()
                       .Allow("https://wchat.freshchat.com/")
                       .Allow("*.webpush.freshchat.com/")
                       .Allow("data:");
                //builder.Object
                //       //.AllowSelf()
                //       .Allow("data:")
                //       .Allow("'unsafe-eval'");

            });

            app.UseSession();
            //app.UseCookiePolicy(new CookiePolicyOptions
            //{
            //    HttpOnly = HttpOnlyPolicy.Always,
            //    Secure = CookieSecurePolicy.Always,
            //    MinimumSameSitePolicy = SameSiteMode.None

            //});
            app.UseAuthentication();
            app.UseCors(options => options.AllowAnyOrigin());
            app.UseResponseCompression();
            //Antiforgery token secure
            //app.UseAntiforgeryTokens();
            app.UseHttpsRedirection();

            app.UseStatusCodePagesWithReExecute("/Index");
            app.UseStaticFiles();
            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}");
            });
        }



        /// <summary>
        /// Mapping all the buisness and repository dependencies
        /// </summary>
        /// <param name="services"></param>
        private void DependencyMapping(IServiceCollection services)
        {
            services.AddSingleton<HttpClientHelper>();
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddSingleton<IConfiguration>(Configuration);
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
        }
    }
    //Global level antiforgery token security to stop malicious scripts
    public static class ApplicationBuilderExtensions
    {
        public static IApplicationBuilder UseAntiforgeryTokens(this IApplicationBuilder app)
        {
            return app.UseMiddleware<ValidateAntiForgeryTokenMiddleware>();
        }
    }
}
