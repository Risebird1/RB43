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

> **No procedure change is needed for the two rubric sections.** The rubric reaches the assistant
> from `usp_GetAIInterviewDetailsBySessionId`, whose rubric result set is
> `select * from TransTechSkill` — so `SkillSet` was always in it. The gap was in C#:
> `AIInterviewerRepository` did not read the column, and now does.
> `Migrations/AddSkillSetToAIInterviewRubric.sql` carries the verification queries and the
> explanation. `usp_GetAIInterviewSpecificDetails` is a different, smaller procedure (prompt ids
> and thread id, one result set) and is correct as it stands.

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

## Making a bad report loud instead of empty

Until an evaluator prompt exists, a screening report renders with empty section blocks. That is
also exactly how it renders when an evaluator *does* exist but drifts — renames a rubric field,
returns percentages, invents a fifth verdict. The two failures were indistinguishable and neither
logged anything, so three things now separate them.

**The kill switch reports itself.** `UseAIInterviewerResponsesApiAsync` in `OpenAIApiController`
needs both `OpenAI:UseResponsesApi` and a non-empty `OpenAI:AIInterviewerPromptId`. Either one
missing silently reroutes every adaptive interview in the environment onto the legacy assistants
path, where channel-level prompt ids are not consulted at all and screening reports cannot produce
section scores. Each reason is now logged once per process — once, because this is consulted on
every turn of every interview and logging each time would bury it.

**A report that cannot be read says so.** Three cases used to pass silently and now log at Error
with the session id and the evaluator prompt id that produced them:

- the evaluator returned nothing, so no report was stored at all;
- the reply parsed as neither an adaptive verdict block nor an evaluation, so `InterviewStatus`
  and the scores are both unset;
- the reply parsed but carried no `rubric_score_summary`, so no parameter can be scored.

These are on regardless of the schema switch below.

**Optionally, the shape can be enforced.** `OpenAIResponseSchemas.AIInterviewEvaluation()` is the
strict JSON schema for the evaluation payload, passed on the closing report turn only — the
interviewing turns return a question, not an evaluation, so only the report can be held to a
shape. `OpenAIResponseSchemas` and `AIInterviewEvaluation` describe the same payload from opposite
sides, and `OpenAIResponseSchemasTests` fails if they drift apart.

It is gated on **`OpenAI:UseStrictJsonSchema`, which defaults to false**, in
`Risebird.Services/appsettings.json` — that is the copy that matters, since the report is generated
there. Off by default because it constrains *every* AI interview report on the platform, not only
screening ones: turn it on, run one interview end to end, confirm the report reads, then leave it
on. One detail cannot be verified without a live call — the schema carries the field name
`"overall_communication_score reasoning"`, with a space, because that is what the parser expects.
If the API rejects a property name containing a space, that is the thing to look at first, and
switching the flag back off restores the previous behaviour exactly.

**No new column was added.** An `InterviewComments.EvaluatorReportJson` was considered and is
redundant: in the structured-JSON path `rawReply` is never rewritten before being stored, so
`ReportOfAIInterviewByAssistant` already holds the evaluator's verbatim output at `Replies[0]`.
(The HTML path does rewrite it, stripping the verdict comment — but that path has no section
scores to diagnose.) A malformed report is therefore already recoverable from the existing column.

## A failed recording no longer ends the interview

Every audio failure used to return `ExitModalPartial`, whose only control is a Close button bound
to `window.close()`. Because the answer post is a full form submit, the page carrying the
interview was already gone by then — so a candidate whose upload did not attach, whose recording
was clipped, or whose transcription came back as noise had no way back in, even in principle.
That is one microphone glitch costing a whole interview.

Those paths now return the candidate to the same question with an explanation. Eleven call sites
across all three answer branches moved from exit to retry:

- nothing attached, and no typed answer either
- recording under 500 bytes
- speech-to-text threw
- `IsTranscriptionValid` rejected the transcript
- the pre-save checks for a missing answer or a video that had not finished uploading

Four stayed terminal, because a retry cannot repair them: session expired, interview not found, no
question on the post, no interview id.

`RetryAnswerAsync` re-renders `Index` with the posted model. The clock keeps its **server-side**
value, so a retry costs the candidate time rather than the interview. Only the answer is cleared —
leaving a stale transcript in the form would let the failed attempt be resubmitted behind them.
The chat log is rebuilt with just the current question: each branch builds its own log on the way
to a *new* question, and this render is the same question again, so without that the candidate
returns to a blank conversation and cannot see what they were asked.

**`BotInterviewQuestionVideos.TranscriptTakeCount`** records how many recordings an answer took.
It is passed as its own argument rather than on `CandidateTranscriptReview`, because a retry
happens whether or not the channel has review switched on and that record only exists when it is.
The counter resets after a successful save, or it would ride along on the model and every later
question would inherit the takes of the one before it. Read it as an equipment signal: a channel
whose takes run high across many candidates has a problem of its own.

## Transcript review is now per channel

`AIInterview:CandidateTranscriptReview` was one switch for every channel in the environment.
Review costs the candidate time on the clock, which suits a screening conversation and does not
always suit a tightly timed technical round — a per-channel decision.

`IRF.CandidateTranscriptReview` holds it, and **NULL means "not decided"** and falls back to the
appsettings switch, so every existing channel keeps exactly today's behaviour. The intake form's
third option, "Use the environment default", writes NULL — it must not write `false`, which would
switch review off for that channel instead of deferring.

The flag is resolved from the channel on the server and never from the posted model. It decides
whether the server trusts a client-supplied transcript instead of transcribing the recording
itself, so a candidate who could post it could skip transcription altogether. `TranscribeAnswer`
looks the channel up for the same reason.

### SQL that must be deployed with this

Code deployed ahead of SQL degrades safely — the repository reads the column through
`Columns.Contains`, so until the procedure is updated every channel reads as "not decided" — but
the feature does nothing until all four land:

1. `Tables/BotInterviewQuestionVideos.sql` — adds `TranscriptTakeCount`.
2. `Tables/IRF.sql` — adds `CandidateTranscriptReview`.
3. `StoredProcedures/usp_SaveBotInterviewQuestionVideos.sql` — new `@TranscriptTakeCount`,
   defaulted and ISNULL-guarded on the UPDATE.
4. `StoredProcedures/usp_GetAIInterviewDetailsBySessionId.sql` — adds `irf.CandidateTranscriptReview`
   to the first result set's column list. **This procedure serves every AI interview**, not only
   screening ones; the change is one column added to a `select` list and touches nothing else.

`usp_SaveAIScreeningProvisioning.sql` also gains `@CandidateTranscriptReview`, but that procedure
is screening-only.

### Not done: live interim transcript

The punch list also asked for the transcript to appear as the candidate speaks. That needs a
streaming speech-to-text session (the realtime API) rather than the single `TranscribeAsync` call
on a finished recording that every answer path uses today — a different transport, a different
failure model, and a rewrite of the recorder on a candidate-facing page. It is deliberately left
out rather than half-built.

## Partner API: a missing template no longer refuses the request

`api/SkillChannel/CreateAndSchedule` is how a partner posts a job title and a candidate list.
Risebird resolves the Assessment Group mapped to that job title, creates a skill channel from the
Group's template, and schedules the candidates. When the Group had no template the request was
refused with "Missing template" — a refusal the partner can do nothing about, since the mapping
exists and the candidates are real; only the template is absent. The branch carried the comment
"if template doesnt exist, call the openAI method to create a tech rubric and create the skill
channel" and a `//temporary` error response underneath it.

That method already existed. `CreateClonableSkillChannelFromMAG` generates a rubric through
`GenerateTechRubric(assessmentGroupName)` and creates a template from it, and is what the bulk
`CreateTemplatesForAllMAGsAsync` path uses. The stub now calls it, then re-reads the template
through the same `usp_CheckExistenceofSkillChannelAgainstRequestId` the caller already trusted —
that procedure picks the newest template on the Assessment Group, which is the one just made.

The template-exists branch was not duplicated. It now reads from local `templateId` /
`templateName` that are either what the lookup returned or what generation produced, so one code
path creates the channel either way.

**Deliberately a conventional template, not an AI Audio screening channel.** The screening intake
could also fill this gap, but it would hand the partner a different product: a rubric auto-accepted
with no recruiter review, no obvious source for `clientUserIds`, a `jdLink` that may be empty, and
reports with no section scores until the evaluator prompt exists. The conventional path gives the
partner exactly what they get today, minus the failure.

### Switched off by default

`SkillChannel:GenerateMissingTemplateFromAssessmentGroup` in `Risebird.Services/appsettings.json`,
default `false`. With it off the endpoint behaves byte-for-byte as before. Two things to weigh
before switching it on:

- The generation is an OpenAI call made **inside the partner's HTTP request**, so that request gets
  slower. Check it against the partner's client timeout.
- Two requests arriving together for the same untemplated Assessment Group will each generate a
  template. The newest wins for both — untidy rather than broken.

`TryCreateMissingTemplateAsync` returns `(0, null)` whenever generation is off, fails, or produces
nothing readable, and the caller then refuses the request exactly as it always did. Nothing in it
throws: a generation failure must cost the request its template, not its error message. All three
failure modes log with the Assessment Group and job title.

### No automated test

`Risebird.Services` has no test project, and this is controller orchestration over nine injected
services, so there is nothing to assert against without building one. Verified by build and by
checking the dependency closure instead: `SkillChannelController` is the **first** consumer of
`IClonableSkillChannelTemplateService` in this app, so that registration had never been exercised.
It resolves — its eight dependencies are registered in `Program.cs`, except
`ITemplateListingSearchRepository`, which comes from `AddRisebirdAzureSearch` — and every one of
those bottoms out at `IConfiguration`, `IApplicationErrorLogger` or a registered repository.

## Retention: the intake stops leaking uploads

An abandoned intake leaked two files for ever. The draft itself was always fine — it lives in the
distributed cache under `AIScreening:DraftLifetimeHours` and expires on its own — but its uploads
do not, so every intake somebody started and walked away from left a job description and a voice
recording in storage with nothing pointing at them.

`AIScreeningBlobPurgeJob` (Azure Functions, 02:40 UTC daily) clears them.

### Why most of this is about what *not* to delete

The two blob families are not equally safe:

| | container | name | referenced afterwards? |
|---|---|---|---|
| Instruction recording | `audios` | `ai_screening_instruction_{draftId}{ext}` | never |
| Job description | `resumes` | `{draftId}_{originalFileName}` | **`IRF.JDLink`, for ever** |

The recording is unambiguous. The job description is not, for two independent reasons: a JD that
became a channel is referenced permanently, and `resumes` also holds **real candidate resumes**,
which `AccountController` uploads under whatever file name the candidate chose.

So a job description is deleted only when **both** hold:

1. its name matches `^d_[0-9a-fA-F]{5}_.+$` — the exact shape the intake generates, since a draft
   id is `"d_"` plus five hex characters; and
2. nothing in `IRF.JDLink` names that blob.

Neither test is sufficient alone, and that is the point. The name test does not protect a
referenced JD. The reference test does not protect a candidate resume — a resume was never in
`IRF.JDLink` to begin with, so by that test alone every resume in the container looks disposable.
A loose prefix of `d_` would have matched a resume called `d_final_v2.pdf`.

Blobs are matched **by name, not by whole url**: `IRF.JDLink` is stored without a SAS token but
handed around with one, and the host differs between environments, so comparing urls would miss a
reference and delete a live JD.

### Three refusals that are deliberate

- **The reference list cannot be read** → the whole run aborts and logs an error.
  `GetReferencedJdLinksAsync` returns `null` on failure rather than an empty list precisely so the
  caller can tell "could not read" from "read, found none". A purge that cannot see what is in use
  must do nothing.
- **A blob's age is unknown** → kept.
- **`RetentionDays` is zero or negative** → falls back to the 7-day default instead of being
  honoured, which would otherwise delete an intake still open on someone's screen.

The query is inline SQL, not a stored procedure, for the same reason: an undeployed procedure
would return nothing, and "nothing is referenced" is the one wrong answer that causes deletion.
It is also not narrowed to screening channels — reading a few extra rows nightly costs nothing
against deleting a job description still in use.

### Rollout

`AIScreening:BlobPurge` in `Risebird.AzureTimerFunctions/appsettings.json`:

- `Enabled` — **false** by default; nothing is examined or deleted.
- `DryRun` — **true** by default. The job selects and logs candidates without deleting. Leave it
  true for at least one night after enabling, read the candidate list, and only then set it false.
- `RetentionDays` — 7. Must comfortably exceed `AIScreening:DraftLifetimeHours`.

`IAIScreeningRepository` and `IAIScreeningBlobPurgeService` are newly registered in the functions
host; neither was resolvable there before.

### Tests

36 tests in `AIScreeningBlobRetentionTests`, weighted towards what must survive. They found one
real defect while being written: a container-root url such as `.../resumes/` returned `"resumes"`
as a blob name, because the trailing slash was trimmed before the last segment was taken.

Mutation-checked, each against the restored original: loosening the name pattern to a bare `d_`
prefix fails 7, treating an unreadable reference list as empty fails 1, and honouring a zero
retention literally fails 3.

## Prompt text is generated per channel, not written by hand

Operations used to write the two prompt texts themselves and paste them into OpenAI. Two things
went wrong with that. Every channel got whatever wording the person remembered, and the rubric
written into the prompt drifted away from the rubric in the database — which matters more than it
sounds, because `AIScreeningReportMapper` matches the evaluator's `rubric_item` strings to
`TransTechSkill.SkillName`, and a parameter the prompt never mentioned scores zero.

`AIScreeningPromptComposer` now builds both texts from the channel's own parameters: job title,
ideal candidate profile, the recruiter's instruction, duration, and both sections with their
parameter names, weights, expected levels, mandatory flags and concepts.

**The publishing loop is unchanged and still manual.** OpenAI has no API that creates a stored
prompt and returns a `pmpt_…` id — that is what the earlier prompt-composer work got wrong and why
it was reverted. This composes text only. A person still pastes it, publishes it, and brings the id
back to the form. What it removes is the hand-authoring, not the human.

### Where it appears

| | Surface | Behaviour |
|---|---|---|
| New channel | `/AIScreening/New` step 4 | Composed on arrival at the step, so a plan revised at step 3 is already in the text. A **Regenerate** button recomposes. |
| Existing channel | Edit IRF | Behind a **Generate prompt instructions** button. |

Edit IRF is opt-in rather than always rendered because that form serves every channel type and
only screening channels have the two sections these prompts are built from. `IsAIAudioScreening`
is not on `IRFVm` and adding it would have meant another read path; a non-screening channel that
clicks the button is told plainly that its rubric carries no section, which is the honest answer.

The endpoints are `GET /AIScreening/Prompts?draftId=` and `GET /AIScreening/PromptsForChannel?irfIntId=`.
Both compose on every call and persist nothing — there is no stored copy to go stale.

`PromptsForChannel` reads through `GetEditIRF`, because `GetSkillChannelDetailsForUpdate`
underneath it is the read that populates `IdealCandidateProfile`; the lighter lookups do not carry
it. It leaves `Instruction` empty rather than inventing one: the spoken instruction lived on the
draft, which expired long ago, and putting words in the recruiter's mouth would be worse than
omitting them.

### The parameters version stamp

Each text carries `# Parameters version: <8 hex>`, a hash of everything it was built from. It
travels into OpenAI with the prompt, so an operator can compare what is published against what the
form shows and see whether the channel has been edited since. Changing the job title, profile,
instruction, duration, any weight or any parameter name moves it.

This is a visual aid, not enforcement. Nothing stores the fingerprint against the channel, so
nothing *blocks* a stale prompt — adding a column for that is a reasonable next step if the stamp
turns out not to be enough in practice.

### Tests

23 in `AIScreeningPromptComposerTests`, weighted towards what silently breaks the report: every
rubric parameter appearing in the evaluator text, names reproduced character for character
(including `C++ / .NET interop (legacy)`), the 1-5 rule, the spaced
`"overall_communication_score reasoning"` field, the four canonical verdicts round-tripping
through `AIInterviewVerdict.Normalize`, and RULE 1 preceding the rubric in the interviewer prompt.
Plus: the interviewer text never contains the report schema, concepts reach the interviewer and
not the evaluator, an undrafted rubric says so rather than emitting an empty one, and no prose line
overruns the wrap column even with a 120-character job title.

Mutation-checked: dropping the rubric list from the evaluator fails 5, renaming the spaced schema
field fails 1, folding unsectioned rows into section one fails 1, and removing weights from the
fingerprint fails 1.
