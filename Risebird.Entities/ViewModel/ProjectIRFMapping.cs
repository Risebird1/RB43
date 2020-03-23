using System;
using System.Collections.Generic;
using System.Text;

namespace Risebird.Entities.ViewModel
{
    public class ProjectIRFMapping
    {
        public int id { get; set; }
        public int ProjectId { get; set; }
        public string ProjectName { get; set; }
        public int IRFId { get; set; }
        public bool IsAdded { get; set; }
        public string IRFName { get; set; }
        public string PrimarySkills { get; set; }
        public string JDTiltle { get; set; }
        public string Client { get; set; }
        public int ClientId { get; set; }
    }
}
