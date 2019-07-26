using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Risebird.Business;
using Risebird.Business.Interface;
using Risebird.CommonHelper;
using Risebird.CommonHelper.Interface;
using Risebird.Repository;
using Risebird.Repository.Interface;

namespace Risebird.Web
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<IConfiguration>(Configuration);
            //Configure session state
            services.AddDistributedRedisCache(options =>
            {
                options.InstanceName = "risebirdportalui";
                options.Configuration = "risebirdportal.redis.cache.windows.net:6380,password=CZQPNXSRw87bjTASEORqVE58vcDaI562tJcSyt8IJnA=,ssl=True,abortConnect=False";
            });
            services.AddAuthentication(sharedOptions =>
            {
                sharedOptions.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                sharedOptions.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
            })
            .AddAzureAd(options => Configuration.Bind("AzureAd", options))
            .AddCookie();
            services.AddSession(o =>
            {
                o.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                o.Cookie.Name = "Risebirdportal.Session";
                o.Cookie.HttpOnly = true;
            });
            services.AddMvc();
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
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
                app.UseExceptionHandler("/Home/Error");
            }

            app.UseStaticFiles();
            app.UseSession();
            app.UseAuthentication();
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
        private static void DependencyMapping(IServiceCollection services)
        {
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
        }
    }
}
