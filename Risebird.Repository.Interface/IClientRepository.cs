using Risebird.Entities.Data;
using Risebird.Entities.ViewModel;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace Risebird.Repository.Interface
{
    public interface IClientRepository
    {
        IEnumerable<Client> GetClients();
        Task<bool> AddClient(Client client);
        Task<bool> UpdateClient(Client entity);
        Task<Client> GetClient(int id);
        Task<bool> DeleteClient(int id);
        Task<IEnumerable<ClientRole>> GetClientRoles();
        Task<string> AddClientJd(ClientJdVM _client);
        IEnumerable<ClientJdVM> FetchClientJd(int clientid);
        string AddCandidateJDScore(int IrfId, List<CandidateCuratedScore> candidateCuratedScore,string candidateProfile);
        IEnumerable<CandidateCuratedScore> GetCandidateJDScore(int id);
    };
}
