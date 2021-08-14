using Newtonsoft.Json;
using Risebird.Business.Interface;
using Risebird.CommonHelper;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using Risebird.Repository.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Risebird.Business
{
    public class ProctorInterviewsService : IProctorInterviewsService
    {
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        private readonly INotificationHelper _notificationHelper;
        private readonly IProctorInterviewsRepository _proctorInterviewsRepository;
        private readonly CancellationToken _cancellationToken;
        private readonly HttpClient _httpClient;


        public ProctorInterviewsService(INotificationHelper notificationHelper, IApplicationErrorLogger applicationErrorLogger, IProctorInterviewsRepository proctorInterviewsRepository, HttpClientHelper factory)
        {
            _applicationErrorLogger = applicationErrorLogger;
            _notificationHelper = notificationHelper;
            _proctorInterviewsRepository = proctorInterviewsRepository;
            _httpClient = factory.GetHttpClient();
        }

        public async Task<IEnumerable<VMProperties>> GetAllVMs()
        {
            try
            {
                var result = await _proctorInterviewsRepository.GetAllVMs();
                return result;
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        public async Task<IEnumerable<VMProperties>> GetAllUserVMs(string emailId, string profile)
        {
            try
            {
                var result = await _proctorInterviewsRepository.GetAllUserVMs(emailId, profile);
                return result;
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        public async Task<IEnumerable<TriggeredVM>> GetAllTriggeredVMs()
        {
            try
            {
                var result = await _proctorInterviewsRepository.GetAllTriggeredVMs();
                return result;
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        public async Task<bool> DeleteTriggeredVMs(List<int> ids)
        {
            try
            {
                var result = await _proctorInterviewsRepository.DeleteTriggeredVMs(ids);
                return result;
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
        }


        //code to get all the existing vms on the azure cloud which are not in the db
        public async Task<List<NonExistingVMDTO>> GetNonExistingVMs()
        {
            List<NonExistingVMDTO> unMatchedList = new List<NonExistingVMDTO>();
            try
            {

                //Get All vm from azure portal existing in subscription

                var path = $"https://risebirdmicroservices.azurewebsites.net/api/GetVMList";
                var functionKey = "fa19fIch6k8mPgiWRkbjvYSpBf26kJffV8jUaXFyWCk2yOXbofkRDA==";

                var response = await _httpClient.SendRequestAsync(HttpMethod.Get, path, null, functionKey, "", _cancellationToken);
                var result = response.Content.ReadAsStringAsync().Result;
                result = result.Replace("\\r", "\r").Replace("\\n", "\n").Replace("\"", "").Replace("\\", "\"");
                var vmsInCloudPortal = JsonConvert.DeserializeObject<VMServerProperty>(result);
                if (vmsInCloudPortal == null)
                    return null;
                //Get vm from db
                var vmsInDb = await _proctorInterviewsRepository.GetAllVMs();
                //Find vms which are not in db
                foreach (var item in vmsInCloudPortal.value)
                {
                    NonExistingVMDTO obj = new NonExistingVMDTO { ResourceGroupName = item.id.Split("/")[4] };
                    bool isAvailable = vmsInDb != null && vmsInDb.ToList().Exists(a => a.Name.ToUpper() == item.name.ToUpper() && a.ResourceGroupName.ToUpper() == obj.ResourceGroupName?.ToUpper());
                    if (!isAvailable && obj.ResourceGroupName != null)
                    {
                        obj.Name = item.name;
                        unMatchedList.Add(obj);
                    }

                }
            }
            catch (Exception ex)
            {

                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return unMatchedList;
        }


        public async Task<string> AddNonExistingVMs(List<NonExistingVMDTO> vmList)
        {
            try
            {
                var vmPropList = new List<VMProperties>();
                var triggeredVMList = new List<TriggeredVM>();
                if (vmList != null && vmList.Any())
                {
                    foreach (var vm in vmList)
                    {
                        var obj = new VMProperties();
                        obj.Name = vm.Name;
                        obj.ResourceGroupName = vm.ResourceGroupName;
                        obj.IsAdded = true;
                        obj.Status = (int)VMState.creating;
                        vmPropList.Add(obj);
                    }
                    var result = await _proctorInterviewsRepository.AddNonExistingVMs(vmPropList);
                    foreach (var vm in vmList)
                    {
                        //add the vm to Triggered VMs
                        var vmId = _proctorInterviewsRepository.GetVM(vm.Name, vm.ResourceGroupName);
                        if (vmId != 0)
                        {
                            var obj = new TriggeredVM();
                            obj.VMPropertyId = vmId;
                            obj.LastAction = "Added";
                            obj.ExpectedStatus = "";
                            triggeredVMList.Add(obj);

                        }
                        else
                        {
                            continue;
                        }

                    }
                    await _proctorInterviewsRepository.AddBulkVmTrigger(triggeredVMList);
                    return result;
                }


                return "Failed. No VMs selected.";
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return "Failed";
            }
        }

        public async Task<string> ResetPassword(VMResetPassword model)
        {
            try
            {
                string first = "$adsi = [ADSI]" + "\"" + "WinNT://$env:COMPUTERNAME" + "\"" + "\n";
                string second = "$existing = $adsi.Children | where {$_.SchemaClassName -eq 'user' -and $_.Name -eq " + "\"" + model.userName + "\"" + "}" + "\n";
                string third = "$existing.SetPassword(" + "\"" + model.Password + "\"" + ")" + "\n";
                first.Split('\n');
                second.Split('\n');
                third.Split('\n');
                string[] scripts = new string[1];
                scripts[0] = first + second + third;
                string Url = $"/resourceGroups/{model.ResourceGroupName}/providers/Microsoft.Compute/virtualMachines/{model.VmName}";


                var path = $"https://risebirdmicroservices.azurewebsites.net/api/RunPowerShellScript";
                var functionKey = "ZFVWEv3QT43nhUxDKAr3ACbFOaLAzcq8o0QxvAi73hhMzKU7k0M3mw==";
                var dto = new RunPowershellDTO();
                dto.Script = scripts;
                dto.Url = Url;

                var response = await _httpClient.SendRequestAsync(HttpMethod.Post, path, dto, functionKey, "", _cancellationToken);
                var result = response.Content.ReadAsStringAsync().Result;
                if (result.ToLower().Contains("success"))
                {
                    //update password in database
                    await _proctorInterviewsRepository.UpdateVMPassword(model.VmId, model.Password);
                }
                return result;


            }
            catch (Exception ex)
            {

                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        public async Task<bool> UpdateVMNamePassword(int id, string customName, string password, string userName)
        {
            try
            {
                return await _proctorInterviewsRepository.UpdateVMNamePassword(id, customName, password, userName);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
        }

        public IEnumerable<TransVMUser> GetUserList(int vmId, int profileId)
        {
            try
            {
                var result = _proctorInterviewsRepository.GetUserList(vmId, profileId).OrderByDescending(x => x.IsAdded == true).ToList();
                return result;
            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public async Task<bool> UpdateVMUsers(int[] userList, int vmId, int profileId)
        {
            try
            {
                return await _proctorInterviewsRepository.UpdateVMUsers(userList, vmId, profileId);
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }

        public async Task<IEnumerable<ProctorVMQuestions>> GetVMQuestions(int vmId)
        {
            try
            {
                var result = (await _proctorInterviewsRepository.GetVMQuestions(vmId)).OrderBy(x => x.Priority);
                return result.ToList();
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }

        public async Task<bool> UpdateVMQuestions(ProctorQuestionsViewModel model)
        {
            try
            {
                return await _proctorInterviewsRepository.UpdateVMQuestions(model);
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }

        public async Task<bool> UpdateActionPerformed(int vmId, string emailId, string action)
        {
            try
            {
                return await _proctorInterviewsRepository.UpdateActionPerformed(vmId, emailId, action);
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }

        public async Task<bool> RemoveVM(int vmId)
        {
            try
            {
                return await _proctorInterviewsRepository.RemoveVM(vmId);
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }
    }
}
