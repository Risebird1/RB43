using System;
using System.Collections.Generic;

namespace Risebird.Entities.ViewModel
{
    /// <summary>
    /// A rubric parameter of a preset AI interview channel: a distinct TransTechSkill.SkillName.
    /// ParameterOrder fixes the ask order of questions inside every set.
    /// </summary>
    public class AIQuestionSetRubricParameterVM
    {
        public string SkillName { get; set; }
        public int ParameterOrder { get; set; }
    }

    /// <summary>
    /// One question available to be dealt into a set.
    /// MaxTimeMinutes, MaxScore and Difficulty are optional in this schema and come back null when
    /// the underlying columns are absent, in which case set totals compute as 0.
    ///
    /// IsCodingQuestion feeds set creation so coding questions are spread across papers rather than
    /// piling into one. It is false when the column is absent.
    /// </summary>
    public class AIQuestionPoolItemVM
    {
        public int id { get; set; }
        public string SkillName { get; set; }
        public string Title { get; set; }
        public int? MaxTimeMinutes { get; set; }
        public int? MaxScore { get; set; }
        public string Difficulty { get; set; }
        public bool IsCodingQuestion { get; set; }
    }

    /// <summary>
    /// The two inputs set creation needs, read together from usp_GetAIQuestionPoolForSets.
    /// </summary>
    public class AIQuestionSetPoolVM
    {
        public bool IsPreset { get; set; }
        public IList<AIQuestionSetRubricParameterVM> RubricParameters { get; set; } = new List<AIQuestionSetRubricParameterVM>();
        public IList<AIQuestionPoolItemVM> Questions { get; set; } = new List<AIQuestionPoolItemVM>();
    }

    /// <summary>
    /// One question inside a set. Identity order (ItemId) is ask order; never re-sort a list of these.
    /// </summary>
    public class AIQuestionSetItemVM
    {
        public int ItemId { get; set; }
        public int SetId { get; set; }
        public string SetName { get; set; }
        public int QuestionId { get; set; }
        public string SkillName { get; set; }
        public string QuestionText { get; set; }
    }

    /// <summary>
    /// One paper: a balanced set holding exactly one question per rubric parameter.
    /// </summary>
    public class AIQuestionSetVM
    {
        public int SetId { get; set; }
        public int IRFId { get; set; }
        public string SetName { get; set; }
        public int TotalMinutes { get; set; }
        public int TotalPoints { get; set; }
        public string PromptId { get; set; }
        public string PromptVersion { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? CreatedOn { get; set; }
        public int AssignedInterviews { get; set; }
        public IList<AIQuestionSetItemVM> Items { get; set; } = new List<AIQuestionSetItemVM>();
    }

    /// <summary>
    /// A row written into the AIQuestionSetItems_TableType bulk parameter. SkillName carries the
    /// question's own SkillName as read from the database, never the model's claim about it.
    /// SortOrder exists only here, never in the table.
    /// </summary>
    public class AIQuestionSetItemTvpRow
    {
        public string SetName { get; set; }
        public int QuestionId { get; set; }
        public string SkillName { get; set; }
        public int? TotalMinutes { get; set; }
        public int? TotalPoints { get; set; }
        public int SortOrder { get; set; }
    }

    /// <summary>
    /// Outcome of a save attempt. On a balance violation Success is false, Message is safe to show a
    /// user, and nothing was written.
    /// </summary>
    public class AIQuestionSetSaveResultVM
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public int SetCount { get; set; }
    }

    /// <summary>
    /// Result of binding an interview to a set.
    ///
    /// The two flags are independent on purpose, because four situations have to be told apart:
    /// not switched on and no sets (the pre-feature flow), switched on with a set (serve from it),
    /// switched on with no sets (a misconfiguration that must fail closed rather than fall back to
    /// the whole pool), and sets present but switched off (the pre-feature flow, worth a warning
    /// because the channel has papers it is not using).
    ///
    /// SetId may be populated even when SetsEnabled is false: an interview already bound keeps its
    /// paper for life, so switching a channel off never moves a candidate mid-interview.
    /// </summary>
    public class AIQuestionSetAssignmentVM
    {
        public int? SetId { get; set; }
        public bool ChannelHasSets { get; set; }
        public bool SetsEnabled { get; set; }
        public string Message { get; set; }
    }

    /// <summary>
    /// Outcome of switching a channel's question sets on or off. Enabled is the state the channel is
    /// actually in afterwards, which is not always what was asked for: switching on is refused when
    /// the channel has no sets yet.
    /// </summary>
    public class AIQuestionSetToggleResultVM
    {
        public bool Success { get; set; }
        public bool Enabled { get; set; }
        public string Message { get; set; }
    }

    /// <summary>
    /// Outcome of removing a sample question.
    ///
    /// A question that has been dealt into a set cannot simply disappear, because the runtime reads
    /// set contents through a join on this question. Success is false when the question belongs to a
    /// set an interview is already locked to. SetsDiscarded is non-zero when the question was dealt
    /// but never used: the channel's now-unbalanced sets were dropped so they can be re-dealt.
    /// </summary>
    public class SampleQuestionDeleteResultVM
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public int SetsDiscarded { get; set; }
    }

    /// <summary>
    /// One question in a launch payload, already in ask order.
    /// </summary>
    public class AIQuestionSetLaunchQuestionVM
    {
        public int ItemId { get; set; }
        public int QuestionId { get; set; }
        public string SkillName { get; set; }
        public string QuestionText { get; set; }
    }

    /// <summary>
    /// The set bound to an interview, with its questions in ask order.
    /// </summary>
    public class AIQuestionSetLaunchVM
    {
        public int InterviewId { get; set; }
        public int SetId { get; set; }
        public int IRFId { get; set; }
        public string SetName { get; set; }
        public int TotalMinutes { get; set; }
        public int TotalPoints { get; set; }
        public string PromptId { get; set; }
        public string PromptVersion { get; set; }
        public DateTime? AssignedOn { get; set; }
        public IList<AIQuestionSetLaunchQuestionVM> Questions { get; set; } = new List<AIQuestionSetLaunchQuestionVM>();
    }

    /// <summary>
    /// What the interview runtime needs to serve the next preset question.
    ///
    /// UsesQuestionSets false means the channel does not deal papers at all, so the pre-feature
    /// picker applies and behaviour is unchanged. When it is true the question may only come from
    /// the bound set: if SetResolved is false the caller must stop, because widening the query to
    /// the whole channel pool would defeat the anti-leak property the sets exist to provide.
    /// </summary>
    public class AIPresetQuestionResolutionVM
    {
        public bool UsesQuestionSets { get; set; }
        public bool SetResolved { get; set; }
        public int? SetId { get; set; }
        public string SetName { get; set; }
        public string PromptId { get; set; }
        public string PromptVersion { get; set; }
        public TechSkillComments Question { get; set; }
        public string Message { get; set; }
    }
}
