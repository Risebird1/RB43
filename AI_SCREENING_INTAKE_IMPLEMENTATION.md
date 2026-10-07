# AI Audio Adaptive HR Screening Intake — implementation notes

Built from the *Screening Intake Screens* UI specification and the RB35 *Conversational Channel
Provisioning* build spec (Rev 4).

A recruiter opens one screen, drops a job description, speaks or types how they want the screen run,
confirms an ideal-candidate profile the system proposes, reviews a drafted two-section rubric, and
clicks once. A screening channel exists, with a candidate link.

The route is `/AIScreening/New`. **The existing skill-channel create flow is not modified**, and
`usp_CreateSkillChannelFromTemplateV1` — shared with the human-panel route — is not modified either.

---

## Before this runs: one step

**Deploy the database changes** (below). That is all — the catch-all Assessment Group is seeded by
the scripts, not created by hand, and nothing else has to be configured for the feature to work.

**Optional:** set `AIScreening:PrimaryPromptId` / `AIScreening:EvaluatorPromptId` to pre-fill the
intake form. The recruiter enters both on the form either way. See *Prompt ids* below.

---

## Database changes

Run in this order. All are additive and guarded with the existing `IF NOT EXISTS` migration style.

| File | Change |
| --- | --- |
| `Risebird.DB/Tables/IRF.sql` | `+ IdealCandidateProfile varchar(max) NULL` |
| `Risebird.DB/Tables/ClonableSkillChannel.sql` | `+ IsAIAudioTemplate bit NULL` |
| `Risebird.DB/Tables/TransClonableIRFTechSkill.sql` | `+ SkillSet int NULL` |
| `Risebird.DB/StoredProcedures/usp_CreateAIAudioTemplate.sql` | new |
| `Risebird.DB/StoredProcedures/usp_SaveAIScreeningProvisioning.sql` | new |
| `Risebird.DB/StoredProcedures/usp_UpdateIRFDetails.sql` | `+ @IdealCandidateProfile` (defaulted) |
| `Risebird.DB/Tables/AssessmentGroup.sql` | seeds the `AIInterview` catch-all group |
| `Risebird.DB/Tables/MasterAssessmentGroup.sql` | seeds its MAG row |
| `Risebird.DB/StoredProcedures/usp_GetMAGsWithoutTemplates.sql` | excludes the catch-all |
| `Risebird.DB/Tables/IRF.sql` | `+ IsAIAudioScreening bit NULL` |
| `Risebird.DB/StoredProcedures/usp_GetReportOfAIInterviewByInterviewId.sql` | new |
| `Risebird.DB/Migrations/AddSkillSetToAIInterviewRubric.sql` | **manual** — adds `SkillSet` to the rubric the assistant sees |

> **Do not deploy `StoredProcedures/usp_GetAIInterviewSpecificDetails.sql`.** The copy in this repo
> is stale: it returns one result set where `AIInterviewerRepository` reads three (channel, rubric,
> Q&A). Running it would drop the rubric and Q&A sets and break every AI interview. The one column
> that needs adding is described in `Migrations/AddSkillSetToAIInterviewRubric.sql`; apply it to the
> live definition scripted out of the target database.

No existing query changes behaviour: nothing that reads these tables today selects the new columns,
and the new `usp_UpdateIRFDetails` parameter defaults to null, which keeps the stored profile.

`usp_GetSkillChannelDetailsForUpdate` selects `IRF.*`, so the read side needed no change.

---

## Where the code lives

**Entities**
- `Enum/AIScreeningSectionEnum.cs` — RoleFitment = 1, MotivationToJoinFitment = 2, stored on `TransTechSkill.SkillSet`
- `Helpers/AssessmentGroupMatcher.cs` — nearest Assessment Group by name
- `ViewModel/AIScreeningIntakeModels.cs` — draft, plan, section, revision, channel result
- `ViewModel/AIScreeningIntakeHelper.cs` — prompts, parsing, per-section weights, scoring
- `ViewModel/AIScreeningReportMapper.cs` — evaluator JSON → the structures ClientReportV3 renders

**Repository** — `AIScreeningRepository.cs` (+ interface): the two new procs, template-name
uniqueness, nearest-match AG lookup.

**Business** — `AIScreeningIntakeService.cs` (+ interface): the service contract from the UI spec.

**Web** — `Controllers/AIScreeningController.cs`, `Views/AIScreening/New.cshtml`,
`Helpers/AIScreeningAccess.cs` (who may use it — shared by the controller and the entry-point button).

**Entry point** — a **New AI Screening** button on the Templates toolbar (`Views/Template/List.cshtml`).
That is where this flow would otherwise start: the recruiter goes to `/Template/List` looking for a
template that matches their role, and this is the path for when none does. The button renders only
for the profiles `AIScreeningAccess` allows, so it is never offered to someone it would turn away.

**Tests** — `Risebird.Business.Tests/AIScreeningTests/` — 96 tests, all passing.

---

## Prompt ids

The same two prompt ids a skill channel already carries, used the same way. Both are entered on the
intake form at step 4 and stamped onto the channel:

| | Entered as | Stamped onto | Runs |
| --- | --- | --- | --- |
| Primary Prompt Id | form field (**required**) | `IRF.OpenAIPromptId` | the adaptive interview |
| Interview Evaluator Prompt Id | form field (optional) | `IRF.OpenAIEvaluatorPromptId` | the candidate's report |

The evaluator is optional — left blank, the Primary Prompt Id generates the report too, which is how
every other AI channel already behaves.

`AIScreening:PrimaryPromptId` and `AIScreening:EvaluatorPromptId` only **pre-fill** the form. Whatever
the recruiter submits wins. With the form field blank and no configured default, the global
`OpenAI:AIInterviewerPromptId` is stamped as a last resort and a warning is logged — that prompt is
shared by every AI interview running today, so editing its content changes all of them.

Both remain editable afterwards on Edit IRF, which is where a channel gets corrected without being
rebuilt.

### The interview and the report share one conversation

An adaptive interview and its report run on **one chained conversation**, and a channel with no
Interview Evaluator Prompt Id uses the same prompt for both jobs (`OpenAIApiController` line ~377).
A prompt that emits its evaluation JSON part-way through therefore gets it read aloud to the
candidate by text-to-speech and stored as the question text on the report.

`AIInterviewReplySanitizer` strips report JSON out of conversational turns — and only those; the
report endpoints need it intact — and logs a warning when it fires. In a healthy interview no turn
carries those keys, so it does nothing. **It is a guard, not a fix**: if the warning appears, the
prompt in use is still leaking its report. The fix is a Primary Prompt Id that only asks questions
(draft in `AIScreening_PrimaryPrompt_Draft.txt`), or a separate `AIScreening:EvaluatorPromptId` so
the two jobs stop sharing a prompt.

### If an interview produces nothing, check these two first

`UseAIInterviewerResponsesApi()` (`OpenAIApiController.cs:465`) requires **both**
`OpenAI:UseResponsesApi` to be true **and** `OpenAI:AIInterviewerPromptId` to be non-empty. Fail
either and the call returns `(null, null)` — no exception, no log entry, no question asked. Check
these before debugging anything in this feature.

---

## Assessment Group resolution

Matched on the **Assessment Group's own name**, and on the **nearest** match rather than an exact
one — a job title lifted off an arbitrary JD almost never equals an AG name character for character.

`AssessmentGroupMatcher` normalizes both sides (case, punctuation, word order), drops seniority
words, folds a few role-word synonyms, and then compares what is left both as a set of words and
character by character. So:

| Job title | Resolves to |
| --- | --- |
| `Sr. Backend Engineer (Kafka)` | Backend Developer |
| `Java Engineer` / `Java Programmer` / `Java Dev` | Java Developer |
| `Java Developer II` | Java Developer |
| `Java Devloper` (typo) | Java Developer |
| `Registered Nurse` | *no match — catch-all* |
| `Salesforce CRM Developer` | *no match — catch-all* (a shared role word alone is not enough) |

Two properties this is built around:

- **It never invents an AG.** Below `AIScreening:AssessmentGroupMatchThreshold` (default `0.6`) it
  returns nothing and the catch-all is stamped. A confident wrong AG is worse than no AG, because
  human-panel interviewer routing reads that column.
- **It is deterministic.** Equal scores break by the shorter name then the lower id, so the same job
  title always resolves the same way regardless of row order.

Every resolution is logged with the group it chose and the score, so a run of bad matches is
diagnosable without a repro. Raise the threshold if titles are landing on the wrong group; lower it
if too much falls through to the catch-all.

There is **no `MasterAssessmentGroup` join** in this lookup. The MAG gate exists to guarantee a
staffable interviewer panel, which an AI Audio channel does not have — requiring one here would only
push matchable roles onto the catch-all for no benefit.

### It never blocks channel creation

Which group an AI channel belongs to does not affect how it runs — there is no panel to staff — so
resolution is best-effort throughout. The order is:

1. nearest group by name;
2. `AIScreening:CatchAllAssessmentGroupId`, if explicitly set;
3. the seeded catch-all, looked up **by name** (`AIScreening:CatchAllAssessmentGroupName`, default
   `AIInterview`), so no environment carries its identity value;
4. none — the channel is still created, with a warning logged, and `AssessmentGroupId` left unset
   rather than `0`.

An unresolved group costs visibility, not the channel: the ~19 procs that INNER JOIN on it drop the
channel from AI resume screening, dashboard ribbon counts and the channel edit screen. Setting the
group later on Edit IRF restores all of it.

### The seeded catch-all

`AssessmentGroup.sql` seeds one row named **`AIInterview`**, and `MasterAssessmentGroup.sql` seeds
its MAG row. Both are idempotent and safe to re-run. Two things keep it out of the human-panel
world: `Isinstaavailable = 0` / `IsRelevant = 0` keep it off the relevant-AG lists, and **no
interviewers are bound to it**, so panel routing can never select it.

`usp_GetMAGsWithoutTemplates` excludes it by name. Without that, the catch-all would show up under
**Create Missing Templates** on the Templates page and invite someone to build a curated human-panel
template with an empty panel.

---

## The four rules that were easy to get wrong

**Weights total 100 per section, independently.** Not 100 across both. Each section renders its own
running total in its header, so a revision that unbalances one is visible before submit rather than
at scoring time.

`DmVoiceRubricRevisionHelper` is reused for the diff, weight normalization and the off-topic
message rather than a second differ being written — but every one of its helpers filters to
`SkillSet` 0 or 1, so Motivation rows (SkillSet 2) would be **silently dropped**. `AIScreeningIntakeHelper`
presents a section's rows as SkillSet 1 for the duration of the call and restores them afterwards
(`WithSectionMasked`). There is a test that fails if that masking is removed.

**An off-topic revision is a valid response, not an error.** When an instruction specifies no rubric
change, the plan comes back unchanged with a message, `success` stays `true`, and the screen renders
the message instead of an empty diff.

**Typing bypasses transcription.** `SetInstructionAsync` writes the same field a transcription
would, without touching the speech service.

**The overall figure is not section one's.** Each parameter earns `weight x (score / 5)`, each
section totals 100, and the overall figure is the **mean of both sections** — so a candidate who
fits the role perfectly but will not join does not read as a pass.

---

## Two deliberate deviations from the build spec

**1. The rubric's sections could not survive the template round trip.**

The spec assumed the rubric reaches the channel intact. It does not:
`TransClonableIRFTechSkill` had no `SkillSet` column, and `usp_CreateSkillChannelFromTemplateV1`
copies the rubric into `TransTechSkill` without one. A two-section rubric would have collapsed into
one unsectioned list.

Rather than modify V1 — shared with the human-panel route, and which the spec is explicit about not
touching — a new proc `usp_SaveAIScreeningProvisioning` runs straight after creation and writes what
V1 does not: `SkillSetsevaluate = 2`, the two prompt ids, the ideal candidate profile, and the rubric
with its sections. `SkillSet` was also added to `TransClonableIRFTechSkill` so the template itself
keeps both sections.

> **Watch out:** `CreateSkillChannelUsingTemplateByInterviewLinkPopUpV1` returns
> `Tuple<bool, int>` where the `int` is the proc's **`@flatRate` output — not the new row's id**.
> `usp_CreateSkillChannelFromTemplateV1` never returns an identity. The channel id has to be read
> back with `GetSkillChannelDetailsByName(irfId)`, which is what the existing create-from-template
> action does (it passes `result.Item2` on as the flat rate). Treating it as the id points
> provisioning at whatever unrelated channel happens to have `id` = the flat rate, and nothing
> errors. `AIScreeningChannelCreationTests` pins this down.
>
> The channel code is recorded on the draft the moment the channel exists, before the id lookup, so
> a retry after a half-completed provisioning returns the existing channel rather than minting a
> second one.

**2. The rubric-renewal exclusion is applied in the repository, not the proc.**

`usp_GetAllClonableSkillChannelsToBeRenewed` is called by the code but its `.sql` is not in this
repo, so the spec's "filter these out in that proc" was not available. `ClonableSkillChannelTemplateRepository.ExcludeAIAudioTemplates`
drops AI Audio templates from the renewal set instead, which holds whatever the proc selects. This
matters: the job regenerates a rubric from the template's Assessment Group name, and an AI Audio
template's AG is a catch-all — it would overwrite the recruiter's rubric with garbage. Its trigger
is currently `0 * * * * *`, **every minute**.

---

## What is done, and the one thing that is not

Done and verified by build and tests:

- Screens 1–4: the whole intake, the service contract, the template, the channel, the candidate link
- Screen 4 (Edit IRF): the Ideal Candidate Profile field, read-write, so a channel can be corrected
  in place; `SkillSetsevaluate` and the prompt ids are set by the provisioning path and stay editable
- The matching `AIInterviewType` guard beside the existing `OpenAIPromptId` guard, on both the add
  and the edit paths (the UI spec asked for this)
- Idempotency: `CreateChannelAsync` is idempotent on draft id, and `usp_CreateAIAudioTemplate` is
  idempotent on template name, so a double submit cannot mint two templates or two channels. The
  button is disabled on first click as well.

**The report.** Both halves now reach ClientReportV3, and there is no second report view.

The rubric side already arrived unaided: `MeetingService.GetInteviewReport` carries `SkillSet` into
`TechSkillReport`, `ReportSkillSetandSeperate` splits the two collections, and the view branches on
`SkillSetsevaluate > 1`. The scored side is now mapped in by `AIScreeningReportMapper`, which fills
that same `InterviewReport` -- section one into `TechSkillReportCollection`, section two into
`TechSkillTwoReportCollection` -- rather than a parallel class of its own.

`InterviewController.ApplyAIScreeningReportAsync` calls it straight after the section split, reading
the stored evaluation from `InterviewComments` by interview id. Every step is gated and every failure
path leaves the model untouched, so a report that cannot be enriched still renders exactly as it does
today.

---

## Open questions from the spec, and what was assumed

| Question | What this build does |
| --- | --- |
| Template reuse vs. per-channel | Fresh template per channel; the instruction is part of the template |
| Draft lifetime | 24 hours, `AIScreening:DraftLifetimeHours` |
| Preset mode | Adaptive only — Preset needs a question bank the AI Audio template omits |
| Client / client user / billing owner | Captured as fields beside the chat on step 4 |
| Who can use it | Admin, SupportAdmin, HR, SeniorHR, InterviewerManagement, SeniorInterviewerManagement — the same set as the existing template action. Clients excluded |
| Visibility in `/Template/List` | Not addressed here — `usp_GetAllClonableSkillChannels` is not in this repo. Generated per-JD templates will appear in the curated list until that proc filters on `IsAIAudioTemplate` |

---

## Candidate transcript review

Speech-to-text used to be submitted unseen: the candidate stopped recording, the server transcribed
the audio inside the submit handler, and whatever came back was filed as their answer. A mis-heard
word — "four years" heard as "fourteen years" — was scored as spoken, and nobody could tell.

The candidate now reads the transcript and corrects it before the answer is submitted.

**The flow.** Recording stops → `AIInterview/TranscribeAnswer` transcribes it and returns the text
without saving anything → the text appears in an editable box → the candidate submits. The existing
form post is unchanged apart from two new fields, so everything downstream of the submit works as it
did.

**The timer keeps running** while they read. It is the same clock that runs while they think about
the question; review is not free time and the panel says so.

**What is stored.** `CandidateAnswer` is what the candidate *confirmed* — that is what gets scored.
Three columns on `BotInterviewQuestionVideos` keep what the machine heard:

| Column | |
| --- | --- |
| `RawTranscript` | what speech-to-text returned, never overwritten |
| `TranscriptEditedByCandidate` | 1 when the confirmed text differs |
| `TranscriptDivergencePct` | how much, 0–100 |

Divergence is measured in **words**, ignoring case and punctuation, so fixing a comma reads as no
edit and a changed word reads as one word. A small number is a mis-heard name or figure; a large one
is worth a QC look. `CandidateTranscriptReview` holds that logic and has tests.

**Scope.** Only answers whose text comes from the recording. Coding questions (the code is the
answer), typing and listening assessments (already typed), chatbox mode and the MCQ section are
untouched and still submit exactly as before.

**Failure never costs the answer.** If transcription fails or returns nothing usable, the candidate
is not trapped: the submit falls through to the server, which transcribes the same recording exactly
as it did before this existed. A recording that could not be made out comes back with an empty box
and a prompt to type the answer instead.

**Switch.** `AIInterview:CandidateTranscriptReview` (default `true`). Set it to `false` to go back to
submitting the transcript unseen, without a deploy.

Not built: re-record (one of two takes) and QC flagging on large divergence. The columns those would
use are in place.
