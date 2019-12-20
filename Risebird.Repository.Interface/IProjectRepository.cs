using Risebird.Entities.Data;
using Risebird.Entities.ViewModel;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Risebird.Repository.Interface
{
    public interface IProjectRepository
    {
        IEnumerable<Project> GetAllProjects();
        IEnumerable<ProjectIRFMapping> GetProjectCluster(int projectId);
        IEnumerable<IRF> GetAllIRF();
        Task<bool> AddProject(Project projectDetails);
        Task<bool> DeleteProject(int id);
        Task<bool> UpdateProject(Project projectDetails);
        IEnumerable<Client> GetClients();
        Task<bool> UnArchiveProject(int id);
        IEnumerable<int> GetIRFIdList(int id);
    }
}
