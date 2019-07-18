using System;
using System.Collections.Generic;
using System.Text;

namespace Risebird.Entities.ViewModel
{
    public class IRFVm
    {
        public int id { get; set; }
        public string IRFId { get; set; }
        public string ClientName { get; set; }
        public string JDTiltle { get; set; }
        public string CompanyType { get; set; }
        public int ClientId { get; set; }
        public string JobCountry { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string JDLink { get; set; }
        public int NoOfPosition { get; set; }
        public int NoOfIntterview { get; set; }
        public int RisebirdHr { get; set; }
        public double PerMintuteRate { get; set; }
        public IList<TransSkill> SkillCollection { get; set; }
        public IList<TransSkillGeneral> SkillGeneral { get; set; }
        public IList<TransJoinability> Joinability { get; set; }
        public IList<TransClientUser> ClientCollection { get; set; }
        public IList<TransQualityController> QualityController { get; set; }
        public IList<TransInterviewer> InterviewerCollection { get; set; }
    }
}
