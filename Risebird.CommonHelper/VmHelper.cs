using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data;
using System.Net;
using System.Threading.Tasks;

namespace Risebird.CommonHelper
{
    public class VmHelper : IVmHelper
    {
        private readonly IConfiguration _configuration;
        private readonly IAzureHelper _azureHelper;
        private readonly IAzureStorageHelper _azureStorageHelper;
        //private readonly IProctorInterviewsRepository _proctorInterviewsRepository;

        public VmHelper(IConfiguration configuration, IAzureHelper azureHelper, IAzureStorageHelper azureStorageHelper)
        {
            _configuration = configuration;
            _azureHelper = azureHelper;
            _azureStorageHelper = azureStorageHelper;
        }
        public async Task<string> Fetchavailablelocation()
        {
            string requestUrl = $"locations?api-version={_configuration["AzureSettings:ApiVersionLocation"]}";
            var response = await GetFromAzureARM(_azureHelper.GetAzureCredential(), requestUrl);
            return response;
        }

        private async Task<string> GetFromAzureARM(AzureCredentials credentials, string requestUrl)
        {
            string endpoint = _configuration["AzureSettings:endPoint"];
            if (credentials != null && !string.IsNullOrEmpty(endpoint) && !string.IsNullOrEmpty(credentials.SubscriptionId))
            {
                var client = CreateClient(string.Format(endpoint, credentials.SubscriptionId), credentials);
                var createRequest = CreateRequest(requestUrl, Method.Get);
                var response = await client.ExecuteAsync(createRequest);
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
        private RestClient CreateClient(string EndPoint, AzureCredentials credentials)
        {
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
        public async Task<string> CreateVmMachine(VMProperties model)
        {
            string resourcegroup = model.ResourceGroupName;
            string requestUrl = $"resourcegroups/{resourcegroup}/providers/Microsoft.Resources/deployments/{resourcegroup}_Deployment?api-version={_configuration["AzureSettings:ApiVersionVMList"]}";
            var response = await generateVMmachine(_azureHelper.GetAzureCredential(), requestUrl, model);
            return response;
        }
        private async Task<string> generateVMmachine(AzureCredentials credentials, string requestUrl, VMProperties model)
        {
            string endpoint = _configuration["AzureSettings:endPoint"];
            if (credentials != null && !string.IsNullOrEmpty(endpoint) && !string.IsNullOrEmpty(credentials.SubscriptionId))
            {
                string osdisk = model.Name + "Disk";
                object[] parameters = new object[] { model.Name, model.UserName, model.Password, model.VmLocation, osdisk };
                string rawEmail = _azureStorageHelper.GetBlobData("templates", "parametersFile.json");
                if (!string.IsNullOrWhiteSpace(rawEmail))
                {
                    rawEmail = rawEmail.Replace("{0}", model.Name).Replace("{1}", model.UserName).Replace("{2}", model.Password).Replace("{3}", model.VmLocation).Replace("{4}", osdisk);
                }
                var fileurl = _azureStorageHelper.InsertBlobVMFile("parameters.json", "templates", rawEmail);
                var client = CreateClient(string.Format(endpoint, credentials.SubscriptionId), credentials);
                var templatelocation = _configuration["AzureSettings:templatelocation"];
                var requestBody = new ArmDeploymentRequestMessage()
                {
                    properties = new ArmDeploymentProperties
                    {
                        templateLink = new ArmApilink
                        {
                            uri = templatelocation,
                            contentVersion = "1.0.0.0"
                        },
                        mode = "Incremental",
                        parametersLink = new ArmApilink
                        {
                            uri = fileurl,
                            contentVersion = "1.0.0.0"
                        },
                    }
                };
                var createRequest = CreateRequest(requestUrl, Method.Put, requestBody);
                var response = await client.ExecuteAsync(createRequest);
                if (response != null && (response.StatusCode == HttpStatusCode.OK ||
                                         response.StatusCode == HttpStatusCode.Created))
                {
                    var status = JsonConvert.DeserializeObject<AzureRMResource>(response.Content).properties.provisioningState;
                    if (status.ToLower() == "accepted")
                    {
                        return "Success";
                    }
                    else
                    {
                        dynamic attributeJsonObj = JsonConvert.DeserializeObject<dynamic>(response.Content);
                        var errorcode = ((JContainer)attributeJsonObj)["error"]["code"].ToString();
                        if (errorcode.ToLower() == "resourcecountexceedslimitduetotemplate")
                            return $"{errorcode} : error occurred, group subscription quota has been exhausted.Please contact Administrator.";
                        else
                            return $"{errorcode} : error occurred while creating ARM resource for selected template, Please contact Administrator.";
                    }
                }
                else if (response != null && (response.StatusCode != HttpStatusCode.OK || response.StatusCode == HttpStatusCode.BadRequest))
                {
                    dynamic attributeJsonObj = JsonConvert.DeserializeObject<dynamic>(response.Content);
                    var errorcode = ((JContainer)attributeJsonObj)["error"]["code"].ToString();
                    if (errorcode.ToLower() == "resourcecountexceedslimitduetotemplate")
                        return $"{errorcode} : error occurred, group subscription quota has been exhausted.Please contact Administrator.";
                    else if (errorcode.ToLower() == "deploymentactive")
                        return ((JContainer)attributeJsonObj)["error"]["message"].ToString();
                    else if (errorcode.ToLower() == "invalidtemplate")
                        return ((JContainer)attributeJsonObj)["error"]["message"].ToString();
                    else
                        return $"{errorcode} : error occurred while creating ARM resource for selected template, Please contact Administrator.";
                }
                return "There was an error while creating ARM resource for selected template, Please contact Administrator.";
            }
            else
            {
                return null;
            }
        }
        public async Task<string> DeleteVM(string resoucegroup, string vmname)
        {
            string requestUrl = $"/resourceGroups/{resoucegroup}/providers/Microsoft.Compute/virtualMachines/{vmname}?api-version={_configuration["AzureSettings:ApiVersionDeleteVM"]}";
            var response = await VMmachineActions(_azureHelper.GetAzureCredential(), requestUrl, Method.Delete);
            return response;
        }
        public async Task<string> DeleteResourcegroup(string resoucegroup)
        {
            string requestUrl = $"/resourcegroups/{resoucegroup}?api-version={_configuration["AzureSettings:ApiVersionDeleteResourceGroup"]}";
            var response = await VMmachineActions(_azureHelper.GetAzureCredential(), requestUrl, Method.Delete);
            return response;
        }
        public async Task<string> DeallocateVM(string resoucegroup, string vmname)
        {
            string requestUrl = $"/resourceGroups/{resoucegroup}/providers/Microsoft.Compute/virtualMachines/{vmname}/deallocate?api-version={_configuration["AzureSettings:ApiVersionDeleteVM"]}";
            var response = await VMmachineActions(_azureHelper.GetAzureCredential(), requestUrl, Method.Post);
            return response;
        }
        private async Task<string> VMmachineActions(AzureCredentials credentials, string requestUrl, Method methodtype)
        {
            string endpoint = _configuration["AzureSettings:endPoint"];
            if (credentials != null && !string.IsNullOrEmpty(endpoint) && !string.IsNullOrEmpty(credentials.SubscriptionId))
            {
                var client = CreateClient(string.Format(endpoint, credentials.SubscriptionId), credentials);
                var createRequest = CreateRequest(requestUrl, methodtype);
                var response = await client.ExecuteAsync(createRequest);
                if (response != null && (response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.Accepted))
                {
                    return "Success";
                }
            }
            return null;
        }
    }
}
