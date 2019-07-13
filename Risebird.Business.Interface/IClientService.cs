using Risebird.Entities.Data;
using System;
using System.Collections.Generic;
using System.Text;
using Risebird.Entities.ViewModel;
using System.Threading.Tasks;

namespace Risebird.Business.Interface
{
    public interface IClientService
    {
        List<Client> GetClients();
        bool AddClient(Client client);
        bool UpdateClient(Client client);
        Client GetClient(int id);
        bool DeleteClient(int id);
    }
}
