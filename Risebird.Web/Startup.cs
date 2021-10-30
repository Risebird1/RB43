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
                //options.IdleTimeout = TimeSpan.FromMinutes(1440);
                options.IdleTimeout = TimeSpan.FromMinutes(120);
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

            app.UseSession();
            //app.UseCookiePolicy(new CookiePolicyOptions
            //{
            //   // HttpOnly = HttpOnlyPolicy.Always,
            //    //Secure = CookieSecurePolicy.Always,
            //    MinimumSameSitePolicy = SameSiteMode.None

            //});
            app.UseAuthentication();
            app.UseCors(options => options.AllowAnyOrigin());
            app.UseResponseCompression();
            //Antiforgery token secure
            //app.UseAntiforgeryTokens();
            app.UseHttpsRedirection();

            app.UseStatusCodePagesWithReExecute("/error/{0}");
            app.UseStaticFiles();
            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
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
