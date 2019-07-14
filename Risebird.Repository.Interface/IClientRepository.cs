using Risebird.Entities.Data;
using System.Collections.Generic;
using System.Threading.Tasks;
using Risebird.Entities.ViewModel;


namespace Risebird.Repository.Interface
{
    public interface IClientRepository
    {
        IEnumerable<Client> GetClients();
        Task<bool> AddClient(Client client);
        Task<bool> UpdateClient(Client entity);
        Task<Client> GetClient(int id);
        Task<bool> DeleteClient(int id);
        Task<IEnumerable<ClientRoles>> GetClientRoles();
    };
}
