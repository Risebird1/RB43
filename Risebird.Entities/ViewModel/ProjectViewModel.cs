using System;
using System.Collections.Generic;
using System.Text;

namespace Risebird.Entities.ViewModel
{
    public class ProjectViewModel
    {
        public bool IsClientUser { get; set; }
        public string Message { get; set; }
        public IEnumerable<Project> ProjectList { get; set; }
    }
}
