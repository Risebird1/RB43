using Risebird.Business.Interface;
using Risebird.Entities.Data;
using Risebird.Repository.Interface;
using System;
using System.Collections.Generic;
using Risebird.Entities.ViewModel;
using System.Linq;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using System.Threading.Tasks;

namespace Risebird.Business
{
    public class ClientService : IClientService
    {
        private IClientRepository _clientRepository;
        private IApplicationErrorLogger _applicationErrorLogger;

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
            catch (Exception ex)
            {
                return null;
            }
        }

        public bool AddClient(Client client)
        {
            try
            {
                return _clientRepository.AddClient(client).Result;                
            }

            catch(Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }

        public Client GetClient(int id)
        {
            try
            {
                var client = _clientRepository.GetClient(id);

                if (client == null)
                    return null;
                else
                    return client.Result;
            }

            catch(Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }

        public bool UpdateClient(Client client)
        {
            try
            {
                return _clientRepository.UpdateClient(client).Result;
            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }

        public bool DeleteClient(int id)
        {
            try
            {
                return _clientRepository.DeleteClient(id).Result;
            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }
    }
}
