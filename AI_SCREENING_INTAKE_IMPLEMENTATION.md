# AI Audio Adaptive HR Screening Intake — implementation notes

Built from the *Screening Intake Screens* UI specification and the RB35 *Conversational Channel
Provisioning* build spec (Rev 4).

A recruiter opens one screen, drops a job description, speaks or types how they want the screen run,
confirms an ideal-candidate profile the system proposes, reviews a drafted two-section rubric, and
clicks once. A screening channel exists, with a candidate link.

The route is `/AIScreening/New`. **The existing skill-channel create flow is not modified**, and
`usp_CreateSkillChannelFromTemplateV1` — shared with the human-panel route — is not modified either.

---

## Before this runs: two manual steps

1. **Deploy the database changes** (below).
2. **Create the catch-all Assessment Group by hand**, e.g. "AI Audio Adaptive", through the existing
   Assessment Group screen. **Bind no interviewers to it** — that is what keeps human-panel routing
   from ever selecting it. Put its id in `AIScreening:CatchAllAssessmentGroupId`. It is used whenever
   the job title does not resolve to a real group (see *Assessment Group resolution*).

Creating a channel without the catch-all returns a clean error naming the missing setting.

**Recommended, not required:** set `AIScreening:PrimaryPromptId` to a stored prompt dedicated to
screening interviews. See *Prompt ids* below for what happens if you don't.

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

**Tests** — `Risebird.Business.Tests/AIScreeningTests/` — 56 tests, all passing.

---

## Prompt ids

Two prompts, both stamped onto the channel at provisioning time:

| | Stamped onto | Form label | Set from |
| --- | --- | --- | --- |
| Runs the interview | `IRF.OpenAIPromptId` | Primary Prompt Id | `AIScreening:PrimaryPromptId` |
| Writes the report | `IRF.OpenAIEvaluatorPromptId` | Interview Evaluator Prompt Id | `AIScreening:EvaluatorPromptId` |

The evaluator is optional — left blank, the Primary Prompt Id generates the report too.

**The Primary Prompt Id is always written to the channel**, never left to the runtime config
fallback. `IRFController.cs:913` and `:2363` guard the field as mandatory server-side, so a channel
without one cannot be saved afterwards.

If `AIScreening:PrimaryPromptId` is not set, the provisioning path stamps the global
`OpenAI:AIInterviewerPromptId` instead and **logs a warning**. That gets the feature working out of
the box, but it is the prompt every AI interview running today uses — editing its content in the
OpenAI account changes behaviour for all of them. Set a dedicated one before screening channels
carry real candidates.

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

**Not wired: the computed scores on the live report.** The two-section *structure* already reaches
ClientReportV3 unaided — `MeetingService.GetInteviewReport` already carries `SkillSet` into
`TechSkillReport`, and the view already branches on `SkillSetsevaluate > 1` — so a screening
channel's report renders as two sections today. What is not yet connected is
`AIScreeningReportMapper`, which computes each section score and the overall figure from the
evaluator's 1–5 ratings and flags when the model's own overall figure disagrees. The mapper is
written and has tests; it needs to be called where the AI evaluation JSON is merged into the report.

That merge sits in the report path shared by every existing interview type, which is why it was left
as its own change rather than folded into this one.

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
