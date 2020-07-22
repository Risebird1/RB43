using Risebird.Business.Interface;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data;
using Risebird.Entities.Enum;
using Risebird.Repository.Interface;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Risebird.Business
{
    public class ClientService : IClientService
    {
        private readonly IClientRepository _clientRepository;
        private readonly IApplicationErrorLogger _applicationErrorLogger;

        public ClientService(IClientRepository clientRepository, IApplicationErrorLogger applicationErrorLogger)
        {
            _clientRepository = clientRepository;
            _applicationErrorLogger = applicationErrorLogger;
        }

        public List<Client> GetClients()
        {
            try
            {
                return _clientRepository.GetClients().ToList();
            }
            catch (Exception)
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

            catch (Exception e)
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

            catch (Exception e)
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

        public List<ClientRole> GetClientRoles()
        {
            try
            {
                var clientRoles = _clientRepository.GetClientRoles();
                if (clientRoles != null)
                    return clientRoles.Result.ToList();
                else
                    return null;
            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
    }
}
