using Risebird.Entities.Data;
using Risebird.Entities.ViewModel;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Risebird.Business.Interface
{
    public interface IProjectService
    {
        IEnumerable<Project> GetAllProjects();
        IEnumerable<IRF> GetAllIRF();
        Task<bool> AddProject(Project projectDetails);
        Task<bool> DeleteProject(int id);
        Project GetProject(int id);
        bool UpdateProject(Project model);
        IEnumerable<Client> GetClients();
        IEnumerable<ProjectIRFMapping> GetProjectCluster(int id);
        Task<bool> UnArchiveProject(int id);
    }
}
