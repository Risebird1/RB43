using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Risebird.Entities.ViewModel;

namespace Risebird.Business.Interface
{
    /// <summary>
    /// The AI Audio Adaptive HR screening intake: a recruiter drops a job description, speaks or
    /// types how they want the screen run, confirms an ideal-candidate profile, reviews a drafted
    /// two-section rubric, and clicks once to get a screening channel with a candidate link.
    ///
    /// This is a separate path from the existing skill-channel create flow, which is not touched —
    /// that isolation is the point of the feature.
    /// </summary>
    public interface IAIScreeningIntakeService
    {
        /// <summary>Starts a draft. Autosaved from this point; nothing reaches IRF until creation.</summary>
        Task<AIScreeningDraft> CreateDraftAsync(string user);

        /// <summary>Loads a draft, or null when it has expired or never existed.</summary>
        Task<AIScreeningDraft> GetDraftAsync(string draftId);

        /// <summary>
        /// Stores the job description blob, keeps the reference on the draft, and extracts its text
        /// for the prompts. Returns the blob url.
        /// </summary>
        Task<string> AttachJdAsync(string draftId, IFormFile jd);

        /// <summary>
        /// Transcribes a spoken instruction and writes it to the draft. Returns null when the audio
        /// did not produce a usable transcript — the caller keeps the recording and says so.
        /// </summary>
        Task<string> TranscribeInstructionAsync(string draftId, Stream audio, string fileName, bool multilingual);

        /// <summary>
        /// Writes a typed instruction to the same field a transcription would.
        /// Typing must bypass transcription entirely.
        /// </summary>
        Task<bool> SetInstructionAsync(string draftId, string instruction);

        /// <summary>Proposes an ideal-candidate profile from the job description and the instruction.</summary>
        Task<AIScreeningIdealCandidate> ProposeIdealCandidateAsync(string draftId);

        /// <summary>Accepts the recruiter's edited profile. Lands on IRF.IdealCandidateProfile at creation.</summary>
        Task<bool> ConfirmIdealCandidateAsync(string draftId, string idealCandidateProfile);

        /// <summary>
        /// Drafts both scored sections — Role Fitment and Motivation to Join Fitment — with weights
        /// totalling 100 within each section independently.
        /// </summary>
        Task<AIScreeningPlan> GeneratePlanAsync(string draftId);

        /// <summary>
        /// Applies a spoken or typed revision to the drafted plan. An instruction that specifies no
        /// rubric change returns the plan unchanged with a message — that is a valid response, not
        /// an error.
        /// </summary>
        Task<AIScreeningPlanRevision> RevisePlanByVoiceAsync(string draftId, string transcript);

        /// <summary>
        /// Creates the AI Audio template and the screening channel.
        ///
        /// Idempotent on draft id: a double submit returns the channel already created rather than
        /// creating a second one. The UI should still disable the button on first click.
        /// </summary>
        /// <param name="schedulingLinkFactory">
        /// Builds the candidate scheduling link for the generated channel code. Supplied by the
        /// controller because the link is built against the current request host.
        /// </param>
        Task<AIScreeningChannelResult> CreateChannelAsync(
            AIScreeningChannelRequest request,
            string loggedInUser,
            string profile,
            Func<string, Task<string>> schedulingLinkFactory);
    }
}
