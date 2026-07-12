using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Risebird.Repository.Interface;
using Risebird.Repository.Interface.Search;
using Risebird.Repository.Search;

namespace Risebird.Repository
{
    /// <summary>
    /// DI registration for shared Azure AI Search infrastructure and listing repositories.
    /// </summary>
    public static class AzureSearchServiceCollectionExtensions
    {
        /// <summary>
        /// Registers shared Azure Search client factory, listing plugins, and a key-based resolver.
        /// Call once from Web and Azure Functions startup.
        /// </summary>
        /// <remarks>
        /// To add Candidate / Interviewer / User next week:
        /// 1. Create *ListingSearchRepository subclassing AzureListingIndexRepositoryBase.
        /// 2. services.AddScoped&lt;ICandidateListingSearchRepository, CandidateListingSearchRepository&gt;();
        /// 3. services.AddScoped&lt;IListingIndexRepository&gt;(sp =&gt; sp.GetRequiredService&lt;ICandidateListingSearchRepository&gt;());
        /// </remarks>
        public static IServiceCollection AddRisebirdAzureSearch(this IServiceCollection services, IConfiguration configuration)
        {
            var serviceOptions = new AzureSearchOptions
            {
                Endpoint = configuration["AzureSearch:Endpoint"],
                ApiKey = configuration["AzureSearch:ApiKey"]
            };

            services.AddSingleton(serviceOptions);
            services.AddSingleton<IAzureSearchClientFactory, AzureSearchClientFactory>();

            // IRF listing plugin (first consumer of the shared pipeline).
            services.AddScoped<IIrfListingSearchRepository, IrfListingSearchRepository>();
            services.AddScoped<IListingIndexRepository>(sp => sp.GetRequiredService<IIrfListingSearchRepository>());

            // Register additional listings the same way, e.g.:
            // services.AddScoped<ICandidateListingSearchRepository, CandidateListingSearchRepository>();
            // services.AddScoped<IListingIndexRepository>(sp => sp.GetRequiredService<ICandidateListingSearchRepository>());

            services.AddScoped<IListingIndexRepositoryResolver, ListingIndexRepositoryResolver>();

            return services;
        }
    }
}
