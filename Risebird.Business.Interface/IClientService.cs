using Risebird.Entities.Data;
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
    }
}
