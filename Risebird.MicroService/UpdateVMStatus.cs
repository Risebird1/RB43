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
    public class UpdateVMStatus
    {
        private readonly IConfiguration _configuration;
        private readonly IAzureHelper _azureHelper;
        private readonly IProctorInterviewsRepository _proctorInterviewsRepository;
        private readonly INotificationHelper _notificationHelper;
        private readonly string connectionString;

        public UpdateVMStatus(IAzureHelper azureHelper, IConfiguration configuration, IProctorInterviewsRepository proctorInterviewsRepository, INotificationHelper notificationHelper)
        {
            _configuration = configuration;
            _azureHelper = azureHelper;
            _proctorInterviewsRepository = proctorInterviewsRepository;
            _notificationHelper = notificationHelper;
            connectionString = _configuration["ConnectionString:DBConnectionString"];

        }
        [FunctionName("UpdateVMStatus")]
        public async Task Run([TimerTrigger("0 * * * * *")] TimerInfo myTimer, ILogger log)
        {
            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");
            //Get all the VMs whose status' need to be checked
            // all the vms in the trans table are the ones whose status is to be checked
            var vmIds = new List<int>();
            var triggeredVMs = await _proctorInterviewsRepository.GetAllTriggeredVMs();
            foreach (var vm in triggeredVMs)
            {
                //check status

                var powerstate = GetVmStatus(vm.ResourceGroupName, vm.Name, connectionString);
                var statusInDB = ((VMState)vm.Status).ToString().ToLower();

                if (powerstate.Contains("&"))
                {
                    var firststatus = powerstate.Split('&')[0].ToLower();
                    var secondstatus = powerstate.Split('&')[1].ToLower();
                    if (statusInDB == "starting" &&
                              (firststatus == "creating" && secondstatus == "running")
                        || (firststatus == "updating" && secondstatus == "running"))
                    {
                        powerstate = "starting";
                    }
                    else if (statusInDB == "unknown" || statusInDB == "creating" &&
                        (firststatus == "creating" && secondstatus == "running")
                        || (firststatus == "updating" && secondstatus == "running"))
                    {
                        powerstate = "creating";
                    }
                    else if (statusInDB == "running" &&
                        (firststatus == "succeeded" && secondstatus == "running"))
                    {
                        powerstate = "running";
                    }
                    else if (statusInDB == "stopping" &&
                        (firststatus == "creating" && secondstatus == "running")
                        || (firststatus == "updating" && secondstatus == "running"))
                    {
                        powerstate = "stopping";
                    }
                    else
                    {
                        powerstate = powerstate.Split('&')[1];
                        if (string.IsNullOrWhiteSpace(powerstate))
                            powerstate = statusInDB;
                    }
                }



                if (powerstate.ToLower() == "running" && statusInDB.ToLower() == "stopping")
                    powerstate = "stopping";

                //update vm status in repository
                var updatedStatus = Enum.IsDefined(typeof(VMState), powerstate.ToLower()) ? (int)Enum.Parse(typeof(VMState), powerstate.ToLower()) : 3;
                await _proctorInterviewsRepository.UpdateVMStatus(vm.VMPropertyId, updatedStatus);

                if (powerstate.ToLower() != "unknown")//if powerstate is unknown, do not delete from Triggered VM table
                {
                    if ((vm.ExpectedStatus != null && vm.ExpectedStatus.Contains(powerstate.ToLower())) || vm.LastAction == "Added")
                    {
                        //add id to a list
                        vmIds.Add(vm.Id);
                    }
                }



            }

            //delete from triggered table
            if (vmIds != null && vmIds.Count() != 0)
            {
                await _proctorInterviewsRepository.DeleteTriggeredVMs(vmIds);
            }


        }



        public AzureRMResource GetVmResourceFromAzureRG(AzureCredentials credentials, string resourceGroupName,
            string resourceName, string resourceType)
        {
            string requestUrl =
                $"resourceGroups/{resourceGroupName}/providers/{resourceType}/{resourceName}/InstanceView?api-version={_configuration["AzureSettings:ApiVersionStatusVM"]}";
            var response = GetFromAzureRM(credentials, requestUrl);
            if (response == null)
                return null;
            return JsonConvert.DeserializeObject<AzureRMResource>(response);
        }
        public string GetVmStatus(string resourceGroupName, string resourceName, string connectionString)
        {
            string resourceType = "Microsoft.Compute/virtualMachines";
            //Get VM current status
            var resource = GetVmResourceFromAzureRG(_azureHelper.GetAzureCredential(), resourceGroupName, resourceName, resourceType);
            if (resource == null)
                return "Unknown";
            var statusList = resource.statuses;
            if (statusList != null && statusList.Count > 1)
            {
                var statusobj1 = statusList.FirstOrDefault()?.code.Split('/').Last();
                var statusobj2 = statusList.LastOrDefault()?.code.Split('/').Last();
                return statusobj1 + "&" + statusobj2;
            }

            var status = statusList?.FirstOrDefault(x => x.code.StartsWith("PowerState"));
            if (status != null) // provisioning State is Succeeded then continue
                return status.code.Split('/').Last();
            return statusList.Select(x => x.displayStatus).FirstOrDefault();
        }

        private string GetFromAzureRM(AzureCredentials credentials, string requestUrl)
        {
            string endpoint = _configuration["AzureSettings:endPoint"];
            if (credentials != null && !string.IsNullOrEmpty(endpoint) && !string.IsNullOrEmpty(credentials.SubscriptionId))
            {
                var client = CreateClient(string.Format(endpoint, credentials.SubscriptionId), credentials);


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
    }
}
