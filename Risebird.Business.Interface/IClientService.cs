using Risebird.Entities.Data;
using Risebird.Entities.ViewModel;
using System.Collections.Generic;

namespace Risebird.Business.Interface
{
    public interface IClientService
    {
        List<Client> GetClients();
        bool AddClient(Client client);
        bool UpdateClient(Client client);
        Client GetClient(int id);
        bool DeleteClient(int id);
        List<ClientRole> GetClientRoles();
        string AddClientJd(ClientJdVM _client);
        IEnumerable<ClientJdVM> FetchClientJd(int clientid);
        bool AddCandidateJDScore(int IrfId, List<CandidateCuratedScore> candidateCuratedScore);
        IList<CandidateCuratedScore> GetCandidateJDScore(int id);
    }
}
