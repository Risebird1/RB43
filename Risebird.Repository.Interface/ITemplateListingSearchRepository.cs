using Risebird.Entities.ViewModel;
using Risebird.Repository.Interface.Search;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Risebird.Repository.Interface
{
    /// <summary>
    /// Clonable skill channel (template) plugin for Azure AI Search (<c>/Template/Index</c>, <c>/Template/List</c>).
    /// </summary>
    public interface ITemplateListingSearchRepository : IListingIndexRepository
    {
        Task<bool> UpsertTemplateAsync(int clonableSkillChannelId);

        Task<bool> RemoveTemplateAsync(int clonableSkillChannelId);

        /// <summary>
        /// Queries the template index for the Templates list views. Returns null on disable/error so callers
        /// fall back to SQL. <paramref name="filter"/> can be null. <c>filter.Archive == "Yes"</c> includes
        /// soft-deleted templates instead of active ones.
        /// </summary>
        Task<IEnumerable<ClonableSkillChannelVM>> SearchAsync(ClonableTemplatesFilter filter = null, string searchValue = null);
    }
}
