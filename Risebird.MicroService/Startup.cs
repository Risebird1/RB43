using Risebird.MicroService;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using Microsoft.AspNetCore.Authentication;

[assembly: FunctionsStartup(typeof(Startup))]
namespace Risebird.MicroService
{
    public class Startup: FunctionsStartup
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
          


        }
    }
}
