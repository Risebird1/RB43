using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RestSharp;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data;
using Risebird.Entities.Enum;
using Risebird.Repository.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Risebird.MicroService
{
    public class UpdateVMIP
    {
        private readonly IConfiguration _configuration;
        private readonly IAzureHelper _azureHelper;
        private readonly IProctorInterviewsRepository _proctorInterviewsRepository;
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        private readonly string connectionString;

        public UpdateVMIP(IAzureHelper azureHelper, IConfiguration configuration, IProctorInterviewsRepository proctorInterviewsRepository, IApplicationErrorLogger applicationErrorLogger)
        {
            _configuration = configuration;
            _azureHelper = azureHelper;
            _proctorInterviewsRepository = proctorInterviewsRepository;
            _applicationErrorLogger = applicationErrorLogger;
            connectionString = _configuration["ConnectionString:DBConnectionString"];

        }
        [FunctionName("UpdateVMIP")]
        public async Task Run([TimerTrigger("0 * * * * *")] TimerInfo myTimer, ILogger log)
        {
            log.LogInformation($"UpdateVMIP Timer trigger function executed at: {DateTime.Now}");

            var vmsInDb = await _proctorInterviewsRepository.GetAllVMs();
            UpdateAzureVmIPsnOSType(vmsInDb, log);//update ips of the vms
            log.LogInformation($"UpdateVMIP Timer trigger function completed at: {DateTime.Now}");
        }

        public void UpdateAzureVmIPsnOSType(IEnumerable<VMProperties> vmListFromDB, ILogger log)
        {
            var finalVmList = new List<VMProperties>();
            string osType = string.Empty;
            string publicIP = string.Empty;
            string privateIP = string.Empty;

            try
            {
                foreach (var vm in vmListFromDB)
                {
                    if (vm.Status == (int)VMState.running)
                    {
                        //if (string.IsNullOrEmpty(vm.PublicIP) && string.IsNullOrEmpty(vm.PrivateIP))
                        //{

                        //}
                        string resourceType = "Microsoft.Network/networkInterfaces";
                        var credentials = _azureHelper.GetAzureCredential();


                        var VMDetails = GetVMDetails(credentials, vm.ResourceGroupName, vm.Name);
                        if (VMDetails != null)
                        {
                            var nicResourceName = VMDetails?.properties.networkProfile
                              .networkInterfaces.FirstOrDefault()?.id.Split("/").Last();
                            osType = VMDetails?.properties.storageProfile.osDisk.osType;
                            var publicPrivateIp = GetPublicIpName(credentials, vm.ResourceGroupName, nicResourceName, resourceType);
                            string pipname = publicPrivateIp.PipName;
                            privateIP = publicPrivateIp.PrivateIp;
                            if (!string.IsNullOrEmpty(pipname))
                            {
                                publicIP = GetVmDnsNameOrIpAddress(credentials, vm.ResourceGroupName, pipname, resourceType = "Microsoft.Network/publicIPAddresses");
                            }
                            finalVmList.Add(new VMProperties { id = vm.id, OSType = osType, PublicIP = publicIP, PrivateIP = privateIP });

                        }
                    }

                }
                _proctorInterviewsRepository.UpdateVMIP(finalVmList);
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                log.LogInformation($"UpdateVMIP Timer trigger function failed at: {DateTime.Now} with error message: {ex.Message}");
            }

        }

        public VMValue GetVMDetails(AzureCredentials credentials, string rgName, string vmName)
        {
            try
            {
                var apiVersion = _configuration["AzureSettings:AzureVMVersion"];

                string requestUrl =
                    $"resourcegroups/{rgName}/providers/Microsoft.Compute/virtualMachines/{vmName}?api-version={apiVersion}";

                var response = PostToAzure(credentials, requestUrl, Method.Get, null);
                if (response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.Accepted ||
                    response.StatusCode == HttpStatusCode.NoContent)
                {
                    return JsonConvert.DeserializeObject<VMValue>(response.Content);
                }
                else
                {
                    return null;
                }
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        public RestResponse PostToAzure(AzureCredentials credentials, string requestUrl, Method method,
          object postData)
        {
            var tcs = new TaskCompletionSource<RestResponse>();
            var client =
                CreateClient(string.Format(_configuration["AzureSettings:endPoint"], credentials.SubscriptionId),
                    credentials);
            var request = CreateRequest(requestUrl, method, postData);
            var response = client.ExecuteAsync(request).Result;
            if (response.ErrorException != null)
                tcs.TrySetException(response.ErrorException);
            else
                tcs.TrySetResult(response);
            //client.ExecuteAsync(request, (response) =>
            //{
            //    if (response.ErrorException != null)
            //        tcs.TrySetException(response.ErrorException);
            //    else
            //        tcs.TrySetResult(response);
            //});

            return tcs.Task.Result;
        }

        private RestClient CreateClient(string EndPoint, AzureCredentials credentials)
        {
            // Default request
            var client = new RestClient(EndPoint);
            client.AddDefaultHeader("Authorization", "Bearer " + credentials.Token);
            return client;
        }
        private RestRequest CreateRequest(string requestUrl, Method requestMethod, object postData = null)
        {
            var request = new RestRequest(requestUrl, requestMethod) { RequestFormat = DataFormat.Json };
            if (postData != null)
                request.AddParameter("application/json", JsonConvert.SerializeObject(postData),
                    ParameterType.RequestBody);
            return request;
        }

        private dynamic GetPublicIpName(AzureCredentials credentials, string resourceGroupName, string resourceName, string resourceType)
        {
            string pipName = string.Empty;
            string privateIp = string.Empty;
            string rdpPublicPort = string.Empty;
            var pipOrLb = GetResourceInformation(credentials, resourceGroupName, resourceName, resourceType).properties;
            if (pipOrLb.ipConfigurations.First().properties.publicIPAddress == null)//if it has Load Balancer
            {
                //Extract the LB name
                var lbId = pipOrLb.ipConfigurations.First().properties.loadBalancerInboundNatRules;
                var lbName = lbId.First().id.Split('/')[8];
                var natRuleName = lbId.First().id.Split('/').LastOrDefault();

                //Extracting the Public Ip address name from the Lb
                var lb = GetResourceInformation(credentials, resourceGroupName, resourceName, resourceType).properties;
                privateIp = lb.ipConfigurations.First().properties.privateIPAddress;
                pipName = lb?.ipConfigurations?.First()?.properties?.publicIPAddress?.id?.Split('/')?.Last();
                //If Multiple Nat rules are defined
                rdpPublicPort = lb?.inboundNatRules?.Count() > 1 ? lb?.inboundNatRules?.FirstOrDefault(x => x.name == natRuleName)?.properties?.frontendPort.ToString() : lb?.inboundNatRules?.First()?.properties?.frontendPort.ToString();
            }
            else //it has public ip address directly
            {
                pipName = pipOrLb.ipConfigurations.First().properties.publicIPAddress.id.Split('/').Last();
            }
            privateIp = pipOrLb.ipConfigurations.First().properties.privateIPAddress;
            return new { PipName = pipName, RdpPort = rdpPublicPort, PrivateIp = privateIp };
        }

        public AzureRMResource GetResourceInformation(AzureCredentials credentials, string resourceGroupName,
            string resourceName, string resourceType)
        {
            string apiVer = _configuration["AzureSettings:ApiVersionRDP"];

            string requestUrl =
                $"resourceGroups/{resourceGroupName}/providers/{resourceType}/{resourceName}?api-version={apiVer}";
            var response = GetFromAzureRM(credentials, requestUrl);
            return JsonConvert.DeserializeObject<AzureRMResource>(response);
        }

        private string GetFromAzureRM(AzureCredentials credentials, string requestUrl)
        {
            string endpoint = _configuration["AzureSettings:endPoint"];
            if (credentials != null && !string.IsNullOrEmpty(endpoint) && !string.IsNullOrEmpty(credentials.SubscriptionId))
            {
                var client =
                    CreateClient(string.Format(endpoint, credentials.SubscriptionId),
                        credentials);
                var createRequest = CreateRequest(requestUrl, Method.Get);
                var response = client.Execute(createRequest);
                if (response == null || (response.StatusCode != HttpStatusCode.OK))
                {
                    return null;
                }

                return response.Content;
            }
            else
            {
                return null;
            }
        }

        private string GetVmDnsNameOrIpAddress(AzureCredentials credentials, string resourceGroupName, string resourceName, string resourceType)
        {
            var resourceInfo = GetResourceInformation(credentials, resourceGroupName, resourceName, resourceType);
            return resourceInfo.properties.ipAddress;
        }
    }
}
