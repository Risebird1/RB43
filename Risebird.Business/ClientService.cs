using Risebird.Business.Interface;
using Risebird.Entities.Data;
using Risebird.Repository.Interface;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Risebird.Business
{
    public class ClientService:IClientService
    {
        private IClientRepository _clientRepository;
        public ClientService(IClientRepository clientRepository)
        {
            _clientRepository = clientRepository;
        }

        public List<Client> GetClients()
        {
            try
            {
                return _clientRepository.GetClients().ToList();
            }
            catch (Exception e)
            {
                return null;
            }
        }
    }
}
