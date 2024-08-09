using Risebird.Entities.Data;
using Risebird.Entities.ViewModel;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Risebird.Business.Interface
{
    public interface IProjectService
    {
        IEnumerable<Project> GetAllProjects();
        IEnumerable<Project> GetProjects(string emailId, string profile);
        IEnumerable<IRF> GetAllIRF();
        Task<bool> AddProject(Project projectDetails);
        Task<bool> DeleteProject(int id);
        Project GetProject(int id);
        bool UpdateProject(Project model);
        IEnumerable<Client> GetClients();
        IEnumerable<ProjectIRFMapping> GetProjectCluster(int id);
        Task<bool> UnArchiveProject(int id);
        Task<ProjectFunnel> GetFunnelData(int id);
        Project GetProjectEdit(int id);
        IEnumerable<Client> GetClientsList();
    }
}
