using Risebird.Entities.Data;
using System.Collections.Generic;

namespace Risebird.Repository.Interface
{
    public interface IClientRepository
    {
        IEnumerable<Client> GetClients();
    }
}
