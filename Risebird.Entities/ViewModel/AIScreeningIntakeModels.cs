using System;
using System.Collections.Generic;
using System.Linq;
using Risebird.Entities.Enum;

namespace Risebird.Entities.ViewModel
{
    /// <summary>
    /// Server-side state for one AI screening intake, held between "New" and "Create screening
    /// channel". Nothing here is persisted to IRF until the channel is created, so a recruiter can
    /// abandon a draft at any step without leaving a half-built channel behind.
    /// </summary>
    public class AIScreeningDraft
    {
        /// <summary>Opaque id shown as "Draft d_8f31c" and used to key every call in the flow.</summary>
        public string DraftId { get; set; }

        public string CreatedBy { get; set; }
        public DateTime CreatedOn { get; set; }
        public DateTime ModifiedOn { get; set; }

        /// <summary>Blob url of the uploaded job description.</summary>
        public string JdBlobUrl { get; set; }
        public string JdFileName { get; set; }
        public long JdFileSizeBytes { get; set; }

        /// <summary>Plain text pulled out of the JD, used in every prompt in this flow.</summary>
        public string JdText { get; set; }

        /// <summary>Job title extracted from the JD, or typed by the recruiter.</summary>
        public string JobTitle { get; set; }

        /// <summary>Blob url of the recorded instruction, when the recruiter spoke it.</summary>
        public string InstructionAudioUrl { get; set; }

        /// <summary>
        /// The recruiter's instruction. Written by transcription when spoken and written
        /// directly when typed — typing must not route through transcription.
        /// </summary>
        public string Instruction { get; set; }

        /// <summary>Profile proposed from the JD at step 2, before the recruiter edits it.</summary>
        public string ProposedIdealCandidateProfile { get; set; }

        /// <summary>Profile the recruiter confirmed. Lands on IRF.IdealCandidateProfile.</summary>
        public string ConfirmedIdealCandidateProfile { get; set; }

        /// <summary>The drafted two-section rubric.</summary>
        public AIScreeningPlan Plan { get; set; }

        /// <summary>
        /// Set once the channel exists. Makes CreateChannelAsync idempotent on draft id, so a
        /// double submit returns the channel already created rather than creating a second one.
        /// </summary>
        public int CreatedIrfIntId { get; set; }
        public string CreatedIrfId { get; set; }
        public int CreatedTemplateId { get; set; }

        public bool HasJd => !string.IsNullOrWhiteSpace(JdBlobUrl) || !string.IsNullOrWhiteSpace(JdText);
        public bool HasInstruction => !string.IsNullOrWhiteSpace(Instruction);
    }

    /// <summary>The drafted interview plan: exactly two scored sections.</summary>
    public class AIScreeningPlan
    {
        public string RoleLabel { get; set; }

        /// <summary>Total interview minutes, as asked for in the instruction.</summary>
        public int DurationMinutes { get; set; }

        public List<AIScreeningPlanSection> Sections { get; set; } = new List<AIScreeningPlanSection>();

        /// <summary>Message from the last revision — including "instruction was off-topic, rubric unchanged".</summary>
        public string Message { get; set; }

        public int ParameterCount => Sections?.Sum(s => s.Parameters?.Count ?? 0) ?? 0;

        public AIScreeningPlanSection GetSection(AIScreeningSectionEnum section) =>
            Sections?.FirstOrDefault(s => s.Section == section);

        /// <summary>Every parameter across both sections, as rubric rows ready for the database.</summary>
        public List<TransSkill> ToTransSkills(int irfId = 0)
        {
            var skills = new List<TransSkill>();
            if (Sections == null) return skills;

            foreach (var section in Sections)
            {
                foreach (var p in section.Parameters ?? new List<TransSkill>())
                {
                    if (p == null || string.IsNullOrWhiteSpace(p.SkillName)) continue;
                    p.IRFId = irfId;
                    p.SkillSet = (int)section.Section;
                    skills.Add(p);
                }
            }
            return skills;
        }
    }

    /// <summary>
    /// One of the two fixed sections. Weights total 100 within a section, independently of the
    /// other — not 100 across both.
    /// </summary>
    public class AIScreeningPlanSection
    {
        public AIScreeningSectionEnum Section { get; set; }

        /// <summary>"Role Fitment" / "Motivation to Join Fitment".</summary>
        public string Title { get; set; }

        /// <summary>The scored parameters. Stored as rubric rows carrying this section's SkillSet.</summary>
        public List<TransSkill> Parameters { get; set; } = new List<TransSkill>();

        /// <summary>Running weight total, rendered in the section header so an unbalancing revision shows before submit.</summary>
        public int WeightTotal => Parameters?.Sum(p => Math.Max(0, p?.Weightage ?? 0)) ?? 0;

        public bool IsBalanced => WeightTotal == 100;
    }

    /// <summary>The ideal-candidate profile proposed from the job description at step 2.</summary>
    public class AIScreeningIdealCandidate
    {
        public string DraftId { get; set; }

        /// <summary>Editable free text — one line, as the recruiter reads and corrects it.</summary>
        public string Profile { get; set; }

        public string Message { get; set; }
    }

    /// <summary>What a voice or typed revision changed, per section.</summary>
    public class AIScreeningPlanRevision
    {
        public AIScreeningPlan Plan { get; set; }

        public List<string> Added { get; set; } = new List<string>();
        public List<string> Removed { get; set; } = new List<string>();
        public List<string> Modified { get; set; } = new List<string>();
        public List<string> Warnings { get; set; } = new List<string>();

        /// <summary>
        /// False when the instruction did not specify a rubric change. That is a valid response,
        /// not an error: the rubric comes back unchanged with <see cref="Message"/> explaining why.
        /// </summary>
        public bool Changed { get; set; }

        public bool WeightsAdjusted { get; set; }

        public string Message { get; set; }
    }

    /// <summary>Outcome of the single "Create screening channel" click.</summary>
    public class AIScreeningChannelResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }

        public int IrfIntId { get; set; }
        public string IrfId { get; set; }
        public int TemplateId { get; set; }

        /// <summary>Candidate-facing scheduling link for the new channel.</summary>
        public string CandidateLink { get; set; }

        /// <summary>True when the draft had already been submitted and this call returned the existing channel.</summary>
        public bool AlreadyCreated { get; set; }
    }

    /// <summary>What channel creation needs beyond the draft, captured beside the chat.</summary>
    public class AIScreeningChannelRequest
    {
        public string DraftId { get; set; }
        public int ClientId { get; set; }
        public List<int> ClientUserIds { get; set; } = new List<int>();
        public int BillingOwner { get; set; }

        /// <summary>
        /// Stored prompt that conducts the interview. Lands on IRF.OpenAIPromptId — the same
        /// Primary Prompt Id the Edit IRF form carries, used the same way.
        /// </summary>
        public string PrimaryPromptId { get; set; }

        /// <summary>
        /// Stored prompt that writes the candidate's report. Lands on IRF.OpenAIEvaluatorPromptId.
        /// Optional: left blank, the Primary Prompt Id generates the report too, which is how
        /// every other AI channel already behaves.
        /// </summary>
        public string EvaluatorPromptId { get; set; }
    }
}
