using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
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
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Risebird.MicroService
{
    public class GetRDPFile
    {
        private readonly string RdpPort = ":3389";
        private readonly IConfiguration _configuration;
        private readonly IAzureHelper _azureHelper;
        private readonly IProctorInterviewsRepository _proctorInterviewsRepository;
        private readonly IApplicationErrorLogger _applicationErrorLogger;

        public GetRDPFile(IConfiguration configuration, IAzureHelper azureHelper, IProctorInterviewsRepository proctorInterviewsRepository, IApplicationErrorLogger applicationErrorLogger)
        {
            _configuration = configuration;
            _azureHelper = azureHelper;
            _proctorInterviewsRepository = proctorInterviewsRepository;
            _applicationErrorLogger = applicationErrorLogger;

        }
        [FunctionName("GetRDPFile")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "GetRDPFile")] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            int vmId = JsonConvert.DeserializeObject<int>(requestBody);
            var rdpFile = GetRdpResponse(vmId, "Microsoft.Network/networkInterfaces");
            var result = JsonConvert.SerializeObject(rdpFile);
            return new OkObjectResult(result);
        }

        private RdpFileModel GetRdpResponse(int vmId, string resourceType)
        {

            string rdpPublicPort = string.Empty;
            var credentials = _azureHelper.GetAzureCredential();
            RdpFileModel response = null;
            try
            {
                var vmPropertyDetails = _proctorInterviewsRepository.GetVMbyId(vmId);
                if (vmPropertyDetails != null && !string.IsNullOrEmpty(vmPropertyDetails.PublicIP))
                {
                    return GetRdpTextResponse(vmPropertyDetails.Name, vmPropertyDetails.OSType, vmPropertyDetails.PublicIP + RdpPort);
                }
                var VMDetails = GetVMDetails(credentials, vmPropertyDetails.ResourceGroupName, vmPropertyDetails.Name);
                var nicResourceName = VMDetails?.properties.networkProfile
                    .networkInterfaces.FirstOrDefault()?.id.Split("/").Last();
                var OsType = VMDetails?.properties.storageProfile.osDisk.osType;
                var publicIp = GetPublicIpName(credentials, vmPropertyDetails.ResourceGroupName, nicResourceName, resourceType);
                string pipname = publicIp.PipName;
                rdpPublicPort = publicIp.RdpPort;
                var dnsNameOrConnectionIp = GetVmDnsNameOrIpAddress(credentials, vmPropertyDetails.ResourceGroupName, pipname, resourceType = "Microsoft.Network/publicIPAddresses");
                if (!string.IsNullOrEmpty(rdpPublicPort))
                {
                    dnsNameOrConnectionIp += ":" + rdpPublicPort;
                }
                else
                {
                    dnsNameOrConnectionIp += RdpPort;
                }
                response = GetRdpTextResponse(vmPropertyDetails.Name, OsType, dnsNameOrConnectionIp);
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return response;
        }

        private RdpFileModel GetRdpTextResponse(string resourceName, string osType, string dnsNameOrConnectionIp)
        {
            var rdpText = WindowsRdpResponse(dnsNameOrConnectionIp);
            if (osType.ToUpper() == "LINUX")
                return GeneratedRdpFile($"{resourceName}.txt", rdpText);
            else
                return GeneratedRdpFile($"{resourceName}.rdp", rdpText);
        }

        public string WindowsRdpResponse(string dnsNameOrConnectionIp)
        {
            var response = $"full address:s:{dnsNameOrConnectionIp}\nprompt for credentials:i:1" + "\n" +
                           "drivestoredirect:s:*";
            return response;
        }
        public RdpFileModel GeneratedRdpFile(string fileName, string rdpfileresponse)
        {
            var filecontents = new List<byte>();
            foreach (var c in rdpfileresponse)
                filecontents.Add((byte)c);
            return new RdpFileModel(fileName, filecontents);
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
                // var resource = resources.FirstOrDefault(x => x.ResourceName == lbName);

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
