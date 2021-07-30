using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Risebird.Entities.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Risebird.Entities.ViewModel
{
    public class SkillChannelTemplateVM //: SkillChannelTemplate
    {
        public int id { get; set; }

        [DisplayName("Template Name")]
        [Required]
        [Remote(action: "IsTemplateNameInUse", controller: "Template")]
        public string TemplateName { get; set; }

        [DisplayName("Assessment Group")]
        [Required]
        public int? AssessmentGroupId { get; set; }
        public DateTime CreatedOn { get; set; }
        public int CreatedBy { get; set; }
        public DateTime ModifiedOn { get; set; }
        public int ModifiedBy { get; set; }
        public bool IsDeleted { get; set; }
        public IList<TransTemplateTechSkill> TechSkillCollection { get; set; }
        public IList<TransTemplateInterviewer> InterviewerCollection { get; set; }
        public SelectList AssessmentGroupList { get; set; }
        public SelectList Skill { get; set; }
        public SelectList LevelTech { get; set; }
        public string ImageUrl { get; set; }
        public string Status { get; set; }
        public string CreatedByEmailId { get; set; }
    }
}
