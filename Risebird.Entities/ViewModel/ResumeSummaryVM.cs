using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Risebird.Entities.ViewModel
{
    public class ResumeSummaryVM
    {
        [JsonProperty("skills")]
        public SkillsSummary Skills { get; set; } = new SkillsSummary();

        [JsonProperty("work_experience")]
        public List<WorkExperience> Work_Experience { get; set; } = new();

        [JsonProperty("work_gaps")]
        public List<WorkGap> Work_Gaps { get; set; } = new();

        [JsonProperty("academics")]
        public List<Academic> Academics { get; set; } = new();

        [JsonProperty("academic_gaps")]
        public List<AcademicGap> Academic_Gaps { get; set; } = new();

        [JsonProperty("certifications")]
        public List<Certification> Certifications { get; set; } = new();

        [JsonProperty("confidence")]
        public string? Confidence { get; set; }
    }

    public class SkillsSummary
    {
        [JsonProperty("primary_specialization")]
        public string? Primary_Specialization { get; set; }

        [JsonProperty("secondary_specializations")]
        public List<string> Secondary_Specializations { get; set; } = new();

        [JsonProperty("technical_skills")]
        public List<string> Technical_Skills { get; set; } = new();

        [JsonProperty("soft_skills")]
        public List<string> Soft_Skills { get; set; } = new();
    }

    public class WorkExperience
    {
        [JsonProperty("job_title")]
        public string? Job_Title { get; set; }

        [JsonProperty("company_name")]
        public string? Company_Name { get; set; }

        [JsonProperty("employment_type")]
        public string? Employment_Type { get; set; }

        [JsonProperty("start_date")]
        public string? Start_Date { get; set; }

        [JsonProperty("end_date")]
        public string? End_Date { get; set; }

        [JsonProperty("location")]
        public string? Location { get; set; }

        [JsonProperty("domain_or_industry")]
        public string? Domain_Or_Industry { get; set; }

        [JsonProperty("tech_stack")]
        public List<string> Tech_Stack { get; set; } = new();

        [JsonProperty("responsibilities_raw")]
        public List<string> Responsibilities_Raw { get; set; } = new();
    }

    public class WorkGap
    {
        [JsonProperty("gap_type")]
        public string? Gap_Type { get; set; }

        [JsonProperty("start_date")]
        public string? Start_Date { get; set; }

        [JsonProperty("end_date")]
        public string? End_Date { get; set; }

        [JsonProperty("description_raw")]
        public string? Description_Raw { get; set; }
    }

    public class Academic
    {
        [JsonProperty("degree")]
        public string? Degree { get; set; }

        [JsonProperty("institution")]
        public string? Institution { get; set; }

        [JsonProperty("field_of_study")]
        public string? Field_Of_Study { get; set; }

        [JsonProperty("start_date")]
        public string? Start_Date { get; set; }

        [JsonProperty("end_date")]
        public string? End_Date { get; set; }

        [JsonProperty("location")]
        public string? Location { get; set; }

        [JsonProperty("achievements")]
        public List<string> Achievements { get; set; } = new();
    }

    public class AcademicGap
    {
        [JsonProperty("start_date")]
        public string? Start_Date { get; set; }

        [JsonProperty("end_date")]
        public string? End_Date { get; set; }

        [JsonProperty("gap_reason")]
        public string? Gap_Reason { get; set; }

        [JsonProperty("description_raw")]
        public string? Description_Raw { get; set; }
    }

    public class Certification
    {
        [JsonProperty("name")]
        public string? Name { get; set; }

        [JsonProperty("issuing_organization")]
        public string? Issuing_Organization { get; set; }

        [JsonProperty("issue_date")]
        public string? Issue_Date { get; set; }

        [JsonProperty("expiration_date")]
        public string? Expiration_Date { get; set; }

        [JsonProperty("credential_id")]
        public string? Credential_Id { get; set; }

        [JsonProperty("description_raw")]
        public string? Description_Raw { get; set; }
    }
}




