using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Risebird.Entities.ViewModel
{
    public class CodeInviteVM
    {
        public CodeInviteVM()
        {

        }
        public int Id { get; set; }

        [Required(ErrorMessage = "Please enter User Name")]
        [Display(Name = "User Name")]
        [RegularExpression(@"^([a-zA-Z0-9_\.\-])+\@(([a-zA-Z0-9\-])+\.)+([a-zA-Z0-9]{2,4})+$",
        ErrorMessage = "Please Enter Correct User Name")]
        public string userName { get; set; }

        //[DataType(DataType.Date)]
        //[DisplayFormat(DataFormatString = "{0:MM/dd/yyyy}")]
        public DateTime inviteStartTime { get; set; }

        //[DataType(DataType.Date)]
        //[DisplayFormat(DataFormatString = "{0:MM/dd/yyyy}")]
        public DateTime inviteEndTime { get; set; }

        [Display(Name = "Experience")]
        public int experience { get; set; }

        [Display(Name = "Programming Language")]
        //public SelectList ProgrammingLanguage { get; set; }
        public string programmingLanguage { get; set; }

        public string TimeZone { get; set; }

        [Display(Name = "Difficulty Level")]
        //public SelectList DifficultyLevel { get; set; }
        public string difficultyLevel { get; set; }
        [Display(Name = "Created At")]
        public DateTime CreatedAt { get; set; }

        [Display(Name = "Created By")]
        public string CreatedBy { get; set; }

        public string invite_UrL { get; set; }
        public string inviteId { get; set; }
        public SelectList ProgrammingLanguageList { get; set; }
    }
    public class CodeInviteURLRequest
    {
        public string programmingLanguage { get; set; }
        public int experience { get; set; }
        public string difficultyLevel { get; set; }
        public string userName { get; set; }
        public DateTime inviteStartTime { get; set; }
        public DateTime inviteEndTime { get; set; }
        public string zone { get; set; }
    }
    public class CodeInviteURLResponse
    {
        public string inviteUrl { get; set; }
    }
    public class CodeInviteReportVM
    {
        public CodeInviteReportVM()
        {
            codeinvitereport = new List<CodeInviteReportDetails>();
        }
        public string inviteId { get; set; }
        public string submissionId { get; set; }
        public string assignmentId { get; set; }
        public string submittedBy { get; set; }
        public DateTime submittedTime { get; set; }
        public int score { get; set; }
        public int totalAssignmentScore { get; set; }
        public int percentageScore { get; set; }
        public string status { get; set; }
        public string inviteRecording { get; set; }
        public List<CodeInviteReportDetails> codeinvitereport { get; set; }
        public string UserName { get; set; }
        public string ProgrammingLanguage { get; set; }
    }
    public class CodeInviteReportDetails
    {
        public string questionName { get; set; }
        public string questionDescription { get; set; }
        public string candidateSolution { get; set; }
        public int achievedScore { get; set; }
        public int questionScore { get; set; }
        public string status { get; set; }
        public List<CodeInviteReportTestCases> testCaseResults { get; set; }
        public string questionstatus { get; set; }
        public string input { get; set; }
        public string expectedOutput { get; set; }
        public string actualOutput { get; set; }
        public string testcasestatus { get; set; }
        public int questionid { get; set; }
    }
    public class CodeInviteReportTestCases
    {
        public string input { get; set; }
        public string expectedOutput { get; set; }
        public string actualOutput { get; set; }
        public string status { get; set; }
    }
}
