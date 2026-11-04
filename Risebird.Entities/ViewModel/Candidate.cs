using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Risebird.Entities.ViewModel
{
    public class Candidate
    {
        public int id { get; set; }
        public int CandidateMasterId { get; set; }
        public int IRFId { get; set; }

        [Required(ErrorMessage = "Please enter Candidate's Name.")]
        public string Name { get; set; }

        [Display(Name = "Email")]
        [RegularExpression(@"^([a-zA-Z0-9_\.\-])+\@(([a-zA-Z0-9\-])+\.)+([a-zA-Z0-9]{2,4})+$",
        ErrorMessage = "Please Enter Correct Email Address")]
        [Required(ErrorMessage = "Please enter Candidate Email Id.")]
        public string EmailId { get; set; }

        [Display(Name = "Resume Link")]

        public string ResumeLink { get; set; }

        [DisplayName("Resume File(.docx,.doc,.pdf files only)")]
        public IFormFile ResumeFile { get; set; }

        [Display(Name = "Encoded Resume Link")]

        public string EncodedResumeLink { get; set; }
        public string IRFName { get; set; }
        public int TotalInvites { get; set; }
        public int InviteConfirmation { get; set; }


        public string Type { get; set; }

        [DisplayName("Location Submitted For")]

        public string Location { get; set; }
        [DisplayName("Current Location")]

        //[RegularExpression("^[a-zA-Z]{1,20}$", ErrorMessage = "Kindly Enter only letters.")]
        public string CurrentLocation { get; set; }

        public string Skill { get; set; }
        [DisplayName("Work Authorization")]

        public string WorkAuth { get; set; }
        [DisplayName("Current Employer")]
        //[Required]
        public string CurrentEmployer { get; set; }
        [Required]
        //[RegularExpression(@"^[0-9]{9}$", ErrorMessage = "Minimum 9 Digits are required")]
        [MinLength(9, ErrorMessage = "Minimum 9 Digits are required")]
        //[Required(ErrorMessage = "Phone number is required")]
        //[StringLength(13, ErrorMessage = "Minimum 9 Digits are required", MinimumLength = 9)]

        public string Phone { get; set; }

        [DisplayName("Total Experience(years)")]

        public decimal TotalExperience { get; set; }
        [DisplayName("Current Salary(Per Hour in $)")]

        public decimal CurrentSalary { get; set; }
        [DisplayName("Expected Salary(lacs)")]

        public decimal ExpectedSalary { get; set; }

        public decimal Notice { get; set; }
        public DateTime EndDate { get; set; }
        public DateTime StartDate { get; set; }
        public string CalendlyLink { get; set; }
        public string ClientName { get; set; }
        public string ClientCompName { get; set; }
        public string JDTiltle { get; set; }
        public int JobLevel { get; set; }
        public int CompanyType { get; set; }
        public string JDLink { get; set; }
        public string JoinabilityComments { get; set; }
        public string SelectabilityComments { get; set; }
        public string LatestComment { get; set; }
        public double SelectabilityScore { get; set; }
        public double JoinabilityScore { get; set; }
        public int AddedByUserId { get; set; }
        public int? Status { get; set; }
        public string StatusValue { get; set; }
        public int imid { get; set; }
        public int confirmationStatus { get; set; }
        public int ClientPhoneNumber { get; set; }
        public int ChanelOwnerPhoneNumber { get; set; }
        public int MeetingStatus { get; set; }
        public int InterviewStatus { get; set; }
        public string VideoLink { get; set; }
        public DateTime? interviewDate { get; set; }
        public string confirmationStatusDesc { get; set; }
        public string InterviewStatusDesc { get; set; }
        public string MeetingStatusDesc { get; set; }
        public string InterviewerComments { get; set; }
        public string LogoUrl { get; set; }
        [DisplayName("Cost Estimate")]
        public string CostEstimate { get; set; }
        [DisplayName("Profile Type")]
        public int Heuristicsscore { get; set; }
        public string HeuristicsscoreDesc { get; set; }

        public SelectList Heuristicsscorelist { get; set; }

        [Display(Name = "Available for Full-time")]

        public bool AvailableFullTime { get; set; }

        [Display(Name = "Available for Freelancing")]

        public bool AvailableFreeLancing { get; set; }
        public int AttemptsCount { get; set; }
        [Display(Name = "Client HR EmailId")]
        [Required]
        [RegularExpression(@"^([a-zA-Z0-9_\.\-])+\@(([a-zA-Z0-9\-])+\.)+([a-zA-Z0-9]{2,4})+$",
        ErrorMessage = "Please Enter Correct Email Address")]
        public string ClientHRAttemptContact { get; set; }

        [Display(Name = "Staffing Agency EmailId")]
        //[Required]
        [RegularExpression(@"^([a-zA-Z0-9_\.\-])+\@(([a-zA-Z0-9\-])+\.)+([a-zA-Z0-9]{2,4})+$",
        ErrorMessage = "Please Enter Correct Email Address")]
        public string ProfileSource { get; set; }
        [Display(Name = "WhatsApp Number")]
        public string WhatsappNumber { get; set; }
        public int FitmentStatus { get; set; }
        public int PriorityStatus { get; set; }
        public int CompletionStatus { get; set; }
        [Display(Name = "Is Proctor")]
        public bool IsProctor { get; set; }

        public string profileName { get; set; }
        public int Candidateesumeid { get; set; }
        public string PrimarySkillInvite { get; set; }
        public string CompanyLink { get; set; }
        public string CompanyVideoLink { get; set; }
        public string ChannelOwnerEmail { get; set; }
        public string ChannelOwnerMobile { get; set; }
        public string ClientHRAttemptContactMobile { get; set; }
        public int CandidateResumeId { get; set; }// id from CandidateResume table
        public string AddedByEmailId { get; set; }// id from CandidateResume table
        public string SchedulerUniqueid { get; set; }
        public int InterviewId { get; set; }
        public int MailCount { get; set; }
        public int CallsCount { get; set; }
        public int WhatsappCount { get; set; }
        // WhatsApp send buttons: opted out of WhatsApp for this IRF, and schedule invites (C1) used of the 3-per-IRF limit.
        public bool WhatsAppOptedOut { get; set; }
        public int WhatsAppSendsUsed { get; set; }
        public bool IsResumeRedactAttempted { get; set; }
        public bool IsResumeRedactFailed { get; set; }
        public string CodedResumeLink { get; set; }
        public bool IsColdCandidate { get; set; }
        public bool IsAIInterview { get; set; }
        public string SessionId { get; set; }
        public bool HasEmailDroppedOrBounced { get; set; }
        public string CurrentInterviewStatus { get; set; }
        public string PreReportLink { get; set; }
        public string PreReportUsername { get; set; }
        public string PreReportPassword { get; set; }
        public string OpenAIScreeningResponseJson { get; set; }
        public bool CandidateIdverification { get; set; }
        public string CandidateVideo { get; set; }
        public string ConsentForm { get; set; }
        public string ResumeSummary { get; set; }
        public string AIResumeScreeningVerdict { get; set; }
        public string AIScreeningResultJsonV2 { get; set; }
        public int? AIResumeScreeningScore { get; set; }

        // Advanced Resume Screening (CandidateAdvancedScreening), read by usp_GetIRFCandidates.
        public string AdvScreeningStatus { get; set; }
        public decimal? AdvScreeningPercentage { get; set; }
        public string AdvScreeningVerdict { get; set; }
        public string AdvAuthenticityRating { get; set; }
        public int? AdvRubricVersion { get; set; }
        public int? AdvInstructionsVersion { get; set; }
        public int? AdvAttemptCount { get; set; }
        public string AdvErrorMessage { get; set; }
        public bool IsAdvancedResumeScreening { get; set; }
        public int? CurrentRubricVersion { get; set; }
        public int? CurrentInstructionsVersion { get; set; }

        /// <summary>Scored on an older rubric or older instructions than the IRF's current ones.</summary>
        public bool IsAdvScreeningStale =>
            AdvScreeningStatus == "Completed"
            && ((AdvRubricVersion ?? 0) < (CurrentRubricVersion ?? 0)
                || (AdvInstructionsVersion ?? 0) < (CurrentInstructionsVersion ?? 0));
        public string AssessmentGroup { get; set; }
        public bool IsAadhaarVerificationRequired { get; set; }
        public bool IsProctoring { get; set; }

    }
}
