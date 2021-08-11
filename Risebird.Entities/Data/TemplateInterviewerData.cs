using System;
using System.Collections.Generic;
using System.Text;

namespace Risebird.Entities.Data
{
    public class TemplateInterviewerData
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string PrimarySkill { get; set; }
        public string EmailId { get; set; }
        public bool IsAdd { get; set; }
    }
}
