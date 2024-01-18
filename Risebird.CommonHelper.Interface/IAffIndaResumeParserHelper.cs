using System;
using System.Threading.Tasks;

namespace Risebird.CommonHelper.Interface
{
    public interface IAffIndaResumeParserHelper
    {
        Task<Tuple<string, string>> UploadAndParseResume(string fileUrl = null, string skillChannelName = null, bool? isAffindaIndexCreated = null);
        Task<string> UploadResumeForRedaction(string fileUrl = null);
        Task<bool> CreateIndex(string indexName);
        Task<bool> IndexADocument(string documentIdentifier, string indexName);
        Task<string> GenerateEmbeddableResumeSearchUrl(string[] indicesArray);
    }
}
