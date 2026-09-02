# AI Interview: MCQ assessment section

Puts a sectioned, GMAT-patterned MCQ assessment inside an AI Interview. The interview runs on
**portal.risebird.io**; the test runs on **screen.risebird.io** and stays there.

## The two decisions you have to know

### 1. What links the two systems: `InterviewMeetings.SessionId`

The channel-level link already existed: `MCQIRF.PortalIRFId` on the MCQ side holds this channel's
`IRF.IRFId`. That is how the status webhook and phone screening already find each other, and it is
reused unchanged to address the MCQ API.

For the *candidate's* link, the correlation key is **`InterviewMeetings.SessionId`** — the GUID the
portal already keys every part of an AI interview on: transcript (`TranscriptsConv`), answer videos
(`BotInterviewQuestionVideos`), proctoring events, and the saved report. Alternatives considered:

- **`InterviewMeetings.id` (interview id).** A small sequential integer. It correlates fine
  server-to-server, but it is guessable, and the candidate's launch URL would then be trivially
  enumerable. Rejected.
- **Candidate email + IRF id.** Already how `MCQShareList` is keyed. But a candidate can be
  interviewed twice on one channel (re-interview, a second role), and this cannot tell those
  attempts apart. Rejected.
- **A new id minted for the link.** Works, but adds a third identifier to reconcile when
  something goes wrong at 2am. Rejected in favour of the one the interview already has.

So on the MCQ side, `AIInterviewMCQSessions.AISessionId` holds the portal's session GUID, and that
row is the join. Nothing about the candidate is duplicated to make the link.

The session GUID is never put in the browser. The MCQ side mints a separate random **launch token**
which is all the iframe URL carries, so a leaked URL exposes one candidate's test and nothing else.

### 2. Where the MCQ data lives: it stays on the MCQ database

Three options were on the table.

| Option | Verdict |
|---|---|
| Copy the MCQ tables into the portal DB (linked server / sync job) | **Rejected.** Duplicates a schema this database does not own and cannot migrate, so every MCQ-side change becomes a breaking change here. Copies far more than the report needs. |
| Fetch live on every render, store nothing | **Rejected on its own.** The report is also rendered headlessly into a PDF and is read by clients months later. A transient failure would produce a report with a hole in it, and the same report would not render the same way twice. |
| Fetch live, keep the last good response | **Chosen.** |

So: **read-through cache.** The MCQ database remains the source of truth and is never bulk-copied.
The portal asks for one session's report over HTTP when it renders a report, and writes what came
back into `AIInterviewMcqAssessment` — the whole response verbatim in `ReportJson`, plus a handful
of denormalised columns so candidates can be listed and sorted by MCQ score without unpacking JSON
in SQL.

A later render prefers a fresh fetch and falls back to the cached row. That means the report keeps
working when the MCQ host is down, and a PDF generated months later shows what the report showed.
When the cached copy is used, the report **says so** on the page, with the timestamp — nobody
should make a hiring call on a stale figure believing it is current.

Adding a field on the MCQ side needs no migration here: it lands in `ReportJson`.

## Flow

```
Skill channel setup
  IRF.HasMcqAssessmentSection = 1
  IRF.McqAssessmentSections   = 'domain-knowledge,data-interpretation,logical-reasoning'
  IRF.McqAssessmentMinutes    = 40
  One SkillChannelSampleQuestions row with IsMcqAssessmentSection = 1,
    placed where in the interview the section should appear.

Candidate's interview
  AIInterview/Index serves questions in order. When it reaches the flagged row:
    -> renders a briefing overlay immediately (no network call)
    -> candidate presses Start
    -> POST AIInterview/PrepareMcqSection
         -> POST {mcq}/api/v1/ai-sessions/{sessionId}/mcq   (generates the paper; slow, once)
         <- embedUrl
    -> iframe loads screen.risebird.io/RecruiterInterview/AISessionTest?t={token}
    -> candidate sits the test in the real MCQ UI
    -> iframe postMessage: section-complete
    -> the interview form posts with IsMcqAssessmentQuestion=true
         -> GET {mcq}/api/v1/ai-sessions/{sessionId}/mcq/report
         -> cached into AIInterviewMcqAssessment
         -> a plain-language summary is written to the transcript as the answer

Report
  AIInterview/Report renders the MCQ block from a live fetch, or the cache if that fails.
```

### Why the section is a question row rather than a config toggle

`IsMcqAssessmentSection` follows the same pattern as `IsTypingSpeedAssessment`,
`IsListeningComprehension` and `IsCodingQuestion`: one flag on one `SkillChannelSampleQuestions`
row, and the delivery order decides where the section falls. That lets a channel author put the
written test after a warm-up question or two, rather than it being bolted to the start or the end.
The row's `Comment` is what the avatar says to introduce it.

### Why the result goes into the transcript

The interview's evaluator prompt reads the transcript. Writing a summary of the MCQ result there
(`"MCQ assessment section completed: 15 of 20 correct (75%). - Domain Knowledge: 7 of 8 ..."`)
means the final verdict weighs the written test alongside the spoken answers, instead of seeing a
silent gap where a question should be. It is stated in words, not JSON, because the evaluator is
being asked to weigh a result rather than parse a payload.

### Why an iframe

The requirement was that the section must not look foreign to the MCQ UI. An iframe of the actual
page is not an imitation of that UI — it *is* that UI: same stylesheet, same question card, same
navigator, same behaviour. A reimplementation would look right the day it shipped and drift from
then on. The questions and the answer key also stay on the MCQ side; the browser never receives
the answer key, and this application never stores a copy of the paper.

To make that work, `Views/RecruiterInterview/CandidateQuestionpage.cshtml`'s 690-line inline
`<style>` block was extracted verbatim to `wwwroot/css/mcq-exam.css`, which both the standalone
test and the embedded one now link. Change it once, both surfaces change.

## Report layout

Follows the supplied HackerEarth assessment report: score header, section-wise performance bars,
a sections summary table, then a detailed submission report showing every question with the
candidate's choice marked against the correct answer.

Two things from that reference are deliberately **not** reproduced: the global percentile band
("Top 1% out of 1819 candidates") and the rank. Both need a cohort of candidates who sat the same
paper — and these papers are generated per candidate, so there is no cohort. Any number shown there
would be invented. Everything rendered is computed from the candidate's own submission.

It lives in `Views/AIInterview/_McqAssessmentSection.cshtml` and is included by both
`Report.cshtml` (the page) and `GenerateAIReportPDFFile.cshtml` (the PDF). Those two views build
their models separately, so a single partial is what keeps the PDF and the page from disagreeing.

When the cached copy is used, the block says so with a timestamp. When the candidate started but
never submitted, it says that too, and labels the figures as partial.

## Configuration

`Risebird.Web/appsettings.json`:

```jsonc
"McqAssessment": {
  "BaseUrl": "https://screen.risebird.io",   // empty switches the integration off
  "ServiceAccountToken": "..."               // must match RisebirdPortalApi:ServiceAccountToken on MCQ
}
```

MCQ side (`RisebirdMCQ/appsettings.json`):

```jsonc
"McqPublicBaseUrl": "https://screen.risebird.io",
"AIInterviewEmbed": { "AllowedFrameAncestors": "https://portal.risebird.io" }
```

With `BaseUrl` empty, a channel flagged for the section reports it as unavailable and the candidate
is offered a way past it. The interview never fails because of this feature.

## Migrations

Portal (`Risebird.DB`), in order:

1. `Tables/IRF.sql` — adds `HasMcqAssessmentSection`, `McqAssessmentSections`, `McqAssessmentMinutes`
2. `Tables/SkillChannelSampleQuestions_AddAIColumns.sql` — adds `IsMcqAssessmentSection`
3. `Tables/AIInterviewMcqAssessment.sql`
4. `StoredProcedures/usp_SaveAIInterviewMcqAssessment.sql`
5. `StoredProcedures/usp_GetAIInterviewMcqAssessment.sql`
6. `StoredProcedures/usp_GetAIInterviewDetailsBySessionId.sql` — now also returns the three IRF columns
7. `StoredProcedures/usp_GetRandomQuestionForSession.sql`, `usp_GetNextAIQuestionForInterview.sql`
8. `StoredProcedures/usp_SaveBotInterviewQuestionVideos.sql` — new optional `@AllowMissingVideo`

MCQ (`RisebirdMCQ/DataAccess/DataBase`), in order:

1. `Tables/AIInterviewMCQSessions.sql`, `Tables/AIInterviewMCQAnswers.sql`, `Tables/MCQSectionColumns.sql`
2. `Storeprocedure/usp_UpsertAIInterviewMCQSession.sql`, `usp_GetAIInterviewMCQSession.sql`,
   `usp_UpdateAIInterviewMCQSessionStatus.sql`, `usp_SaveMCQSetSections.sql`,
   `usp_AddAIInterviewMCQSet.sql`, `usp_StampAIInterviewMCQAnswers.sql`,
   `usp_SaveAIInterviewMCQAnswer.sql`, `usp_GetAIInterviewMCQAnswers.sql`,
   `usp_GetAIInterviewMCQReport.sql`

All are idempotent. The repository mapping guards the three new `IRF` columns with
`DataTable.Columns.Contains`, so shipping the code ahead of step 6 degrades to "no MCQ section"
rather than throwing on every interview page load.

## Three interview guards the section had to be exempted from

The interview assumes every question produces a recorded answer. The MCQ section produces none —
the candidate answers inside the embedded test — so three places would otherwise have trapped them:

1. **`ValidatePresetAnswerPayloadBeforeSave`** rejected a submission with no `VideoFile`. Now exempt,
   alongside the existing typing / listening / coding exemptions.
2. **`UpdateBotInterviewVideoDetails`** returned `false` on an empty `VideoLink`, and
   `usp_SaveBotInterviewQuestionVideos` silently did nothing — which the caller read as a failed
   save and showed the candidate a network error. A new opt-in `@AllowMissingVideo` (default `0`,
   so every existing caller is unchanged) records the answer without a video.
3. **The GET advancement guard** re-served the last question until an answer video existed, which
   for this section would have looped forever. It now checks whether the last question was the MCQ
   section and skips the video requirement if so. A lookup failure keeps the guard in force, since
   wrongly exempting a spoken question would let the interview advance past an unrecorded answer.

Whether a submission *is* the MCQ section is decided server-side from `CurrentQuestionId`, never
from the posted form — the flag waives answer checks, so a candidate must not be able to assert it
on an ordinary question and skip answering.

## One behaviour change outside this feature

Answer scoring was extracted from `RecruiterInterviewController.CandidateQuestionsD` into
`Helper/McqAnswerGrader.cs` so the standalone test and the embedded one cannot disagree about what
counts as correct. The rules are unchanged, but two long-standing bugs are fixed in the process,
and they affect the **existing standalone MCQ test**:

- The original only ever set `Rating` to `1`, never back to `0`. A candidate who answered correctly
  and then changed to a wrong option kept the mark.
- `QuestionAnswer` was only assigned inside the per-option branches, so clearing every checkbox on
  a multi-select left the previous selection in place.

Both inflated scores. Fixing them is correct, but it does change results for the standalone flow in
those two cases — worth knowing before this ships.

## API reference

`RisebirdMCQ/Docs/AIInterview-MCQ-Section-API.md`.
