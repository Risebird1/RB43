using Microsoft.AspNetCore.Mvc.Rendering;

namespace Risebird.Entities.Data
{
    public class VMProperties
    {
        public int id { get; set; }
        public string vmId { get; set; }
        public string Name { get; set; }
        public string ResourceGroupName { get; set; }
        public string PublicIP { get; set; }
        public int? Status { get; set; }
        public bool IsAdded { get; set; }
        public bool IsRemoved { get; set; }
        public bool IsDeleted { get; set; }
        public string StatusDesc { get; set; }
        public string ImageUrl { get; set; }
        public string LastAction { get; set; }
        public string OSType { get; set; }
        public string PrivateIP { get; set; }
        public string CustomName { get; set; }
        public string Password { get; set; }
        public int QuestionsCount { get; set; }
        public string VmLocation { get; set; }
        public string UserName { get; set; }
        public SelectList LocationsList { get; set; }
        public int shutdowntime { get; set; }
        public int deletetime { get; set; }
        public int VMCount { get; set; }
        public string methodtype { get; set; }
    }
    public class ArmDeploymentRequestMessage
    {
        public ArmDeploymentProperties properties { get; set; }
    }
    public class ArmDeploymentProperties
    {
        public ArmApilink templateLink { get; set; }
        public ArmApilink parametersLink { get; set; }
        public string mode { get; set; }
    }
    public class ArmApilink
    {
        public string uri { get; set; }
        public string contentVersion { get; set; }
    }
}
