# Driving Business / CommonHelper / Repository to full coverage

## What's in place now

```
dotnet build RisebirdPortal.sln
./coverage.ps1          # Windows — per-project line + branch %
./coverage.sh           # bash equivalent
./coverage.ps1 -Html    # adds an HTML report (needs dotnet-reportgenerator-globaltool)
```

`coverage.runsettings` gates collection to `Risebird.Business` and `Risebird.CommonHelper` and
excludes generated code, so the gated percentage is the percentage that matters.
`coverage.repository.runsettings` reports `Risebird.Repository` separately and informationally.

**Every percentage in this document is still derived from source volume, not from a measured run.
One `./coverage.ps1` replaces all of them with real numbers.** Nothing here has been compiled
either — there is no .NET SDK in the environment that produced it — so expect a first-build error
pass; paste the errors and they get fixed.

## New in this pass

| What | Where | Why it matters |
|---|---|---|
| Interviewer-lifecycle fixture | `Business.Tests/Fixtures/InterviewerLifecycleFixture.cs` | Records every collaborator call **in order**, so a test can assert not just what happened but that nothing happened after a failed step. Both deactivation rails are ordered cascades, which is exactly the property that a mock-count assertion misses. |
| Channel deactivation suite | `Business.Tests/AutoQcTests/DmIvChannelDeactivationServiceTests.cs` | Client-report "Remove Interviewer": authorisation, role routing, the Client-Vet and RB-Vet rails, audit row shape, metadata JSON, and every partial-failure stop point. |
| Auto-QC removal suite | `Business.Tests/AutoQcTests/DmIvAutoQcRemovalServiceTests.cs` | The forward trigger, the shared removal rail with all six `AutoQcRemovalOptions` knobs, the duplicate-suppression path, and both ops-initiated IRF-audit rails (remove / keep). |
| Offline HTTP harness | `CommonHelper.Tests/Infrastructure/StubHttpMessageHandler.cs` + `StubHttpClientFactory.cs` | Scriptable responses, transport failures, and bodies captured **at send time** (the production code disposes its content, so reading it later throws). This is the reusable piece — every remaining HTTP helper suite is built on it. |
| Micro-service request extension | `CommonHelper.Tests/HttpSuites/HttpClientExtensionsTests.cs` | Every portal call to the Azure Functions layer goes through `SendRequestAsync`; a missing header there is a 401 that surfaces nowhere near the cause. |
| Plivo voice client | `CommonHelper.Tests/HttpSuites/PlivoServiceTests.cs` | Real outbound calls that cost money and reach candidates: URL, Basic auth, snake_case payload, `NullValueHandling`, attempt counting, and the answer XML. |
| `HttpClientHelper` | `CommonHelper.Tests/HttpSuites/HttpClientHelperTests.cs` | Base-address resolution and its null-guard chain. |
| ElevenLabs webhook signature | `CommonHelper.Tests/Helpers/ElevenLabsWebhookSignatureTests.cs` | The only thing protecting a public webhook that writes screening results. Every negative case is a way in if the check were wrong. |
| Candidate masking | `CommonHelper.Tests/Helpers/CandidateReportMaskingHelperTests.cs` | A privacy control whose whole design exists so that deleting a URL parameter cannot unmask a candidate. |
| Password + MIME sniffing | `CommonHelper.Tests/Helpers/PasswordAndMimeTypeHelperTests.cs` | First-login credentials, and the upload gate that refuses to trust a file extension. |
| OpenAI cost + token math | `CommonHelper.Tests/CommonHelperLogic/OpenAIApiCostTests.cs` | Pure money math over six pricing tiers and seven response shapes. Both halves of the cost dashboard. |

### Defects these tests pin

Added to the fifteen already listed in `UNIT_TESTS_PASS1_README.md`:

16. **`PlivoService.BuildAnswerXml` does not escape its two URLs.** A `statusCallbackUrl`
    carrying a query string (`?a=1&b=2`) produces malformed PlivoXML, so the call answers to
    silence. Fix: escape both interpolated values.
17. **`OpenAIApiLogger.ExtractTokenUsage` lets a zeroed top-level `usage` shadow a real nested
    one.** The `thread.run` branch takes `SelectTokens("$..usage").FirstOrDefault()`, which is
    document order — so a response carrying `usage: {0,0}` plus real per-step figures records as
    free. Fix: take the first `usage` that actually reports tokens.
18. **`HttpClientHelper` has an incomplete null-guard chain.** `options?.Value?.MicroServiceSettings.WebApiUrl`
    throws when the section was never bound. Fix: one more `?`.
19. **The auto-QC removal rail ignores the IRF cascade's return value** while the client-report
    rail treats it as fatal. One of the two is wrong; the tests record the asymmetry so the
    decision is deliberate.
20. **`PlivoService` mutates the caller's request object** when filling in the default `From`, so
    a reused request carries the previous call's caller id.
21. **`PasswordHelper` can emit a two-character-prefix password** (`AB@123`) for a user whose
    first and last names are one character each — shorter than most B2C password policies allow.

## The CommonHelper seam inventory — measured, not estimated

The previous version of this document listed roughly two dozen helpers as "already hold an
injectable client". That was wrong, and it mattered, so here is the actual inventory from reading
every file in the project.

### Fully testable today, no production change (do these next)

| Helper | Size | Seam |
|---|---|---|
| `PlivoService` | 4 KB | `IHttpClientFactory` — **done this pass** |
| `VapiService` | 24 KB | `IHttpClientFactory` — same pattern, straight copy of the Plivo suite |
| `WhatsAppHelper` | 51 KB | All HTTP goes through the injected `IHttpRequestHelper`; mock it and the whole helper is drivable |
| `OpenAIApiLogger` | 16 KB | Pure math over an injected repository — **cost and token halves done this pass**, `LoggedApiCallAsync` still open |
| `HttpClientExtensions` | 1 KB | Pure extension — **done this pass** |
| `HttpClientHelper` | 2 KB | **done this pass** |
| `ElevenLabsAgentService` | 10 KB | Signature verification is pure — **done this pass**; the send path needs the seam below |
| `CandidateReportMaskingHelper` | 3 KB | Pure — **done this pass** |
| `PasswordHelper` | 2 KB | Injected `IB2CClientService` — **done this pass** |
| `FileMimeTypeHelper` | 1 KB | Pure — **done this pass** |
| `PlivoAudioConverter` | 5 KB | Pure — still open |
| `NotificationHelper` | 184 KB | **Partly.** SendGrid is constructed per send, but the helper's template selection, recipient splitting and body building are reachable through the injected `ISendGridRepository`, `IValidationHelper` and `IAzureStorageHelper`. The largest single file in the project and worth its own session. |

That is about 300 KB of the project reachable with no production change — considerably more than
the 45–60% the previous estimate implied, because `WhatsAppHelper` and much of
`NotificationHelper` were miscategorised as unreachable.

### Needs a one-line seam to become testable

These construct their client internally. In every case the change is the same shape as
`PlivoService` already has — take `IHttpClientFactory` (or `HttpClient`) in the constructor
instead of calling `new` — and it is behaviour-preserving.

| Helper | Size | What it constructs |
|---|---|---|
| `AssistantServiceHelper` | 109 KB | `new HttpClient()` in 14 places |
| `OpenAIResponsesHelper` | 25 KB | `new HttpClient()` ×2 |
| `AffIndaResumeParserHelper` | 17 KB | `new HttpClient()` ×4, `new RestClient()` |
| `OpenAIInterviewerAssistantHelper` | 17 KB | `new HttpClient()` — **already has an `HttpClient` constructor parameter, commented out** |
| `ZohoMeetingHelper` | 15 KB | `new RestClient()` ×3 |
| `McqAssessmentService` | 14 KB | `new HttpClient()` |
| `CalendlyHelper` | 13 KB | `new RestClient()` ×5 |
| `TeamsMeetingHelper` | 12 KB | `new HttpClient()`, `new RestClient()` ×3 |
| `PlivoCXService` | 12 KB | `new HttpClient()` |
| `SynthesiaTextToVideoService` | 11 KB | `new HttpClient()`, `new RestClient()` ×2 |
| `VmHelper` | 10 KB | `new RestClient()` |
| `OpenAISpeechToTextService` | 8 KB | `new HttpClient()` — commented-out ctor parameter again |
| `PlivoWhatsAppService` | 7 KB | `new HttpClient()` |
| `B2CClientService` | 7 KB | `new HttpClient()` |
| `ZoomHelper` | 7 KB | `new RestClient()` ×3 — `ZoomToken()` is pure JWT and testable today |
| `ResumeParserHelper` | 6 KB | `new HttpClient()` |
| `InterviewApiService` | 7 KB | `new HttpClient()` — **ctor parameter present and commented out**; `DescribeFailureAsync` is pure but private |
| `HttpRequestHelper` | 2 KB | `static readonly RestClient` — needs the field injected |
| `RBMeetHelper` | 2 KB | `new RestClient()` |
| `CodeInviteRequest` | 1 KB | `new HttpClient()` with certificate validation disabled |

Three of these (`InterviewApiService`, `OpenAIInterviewerAssistantHelper`,
`OpenAISpeechToTextService`) already have the constructor parameter written and commented out.
Uncommenting those three and registering them in DI is the cheapest coverage available in this
project.

### Needs a wrapper interface, not just a client

`AzureStorageHelper` (67 KB), `TwilioHelper` (19 KB — builds a `TwilioRestClient` and passes it
to static resource methods), `GoogleEventHelper`, `AzureFaceApi`, `AWSRekognitionService`,
`SASUrlHelper`, `TrackPublicUrlAccessor`, `SpeechToTextHelper`, `PdfReaderHelper`, `ExcelHelper`,
`AzureHelper`. These bind to a cloud SDK rather than to HTTP, so the seam is a thin interface
over the operations actually used.

## The honest ceiling, per project

### Risebird.Business — 100% is reachable with unit tests

Every dependency is an injected interface, which is why the auto-mock harness works.

- **~45 delegating services**: the sweep plus the hand-written suites should have these near 100%.
- **The three giants are the whole remaining problem.** `IRFService` (262 KB),
  `MeetingService` (234 KB) and `InterviewerService` (111 KB) are ~44% of the project by volume
  and dense with branching. The sweep executes *one* path per method; the rest need hand-written
  cases, feature area by feature area. This is weeks of work, not hours.
- Still wanting a fixture and a per-branch suite: `CashfreePayoutService`, `DashboardService`,
  `CandidateService`, `ClientService`, `UserService`, `AICodeInterviewService`,
  `ClonableSkillChannelTemplateService`, `MCQService`, `InterviewSchedulerService`,
  `EvaluatorService`, `DmIvAutoQcBackfillService`, `TrialFlagReviewService`,
  `QcInterviewAIMergeJobService`.

**Estimated now: 65–78% · achievable with unit tests alone: 95%+.**

### Risebird.CommonHelper — the reachable share is larger than previously stated

**Estimated now: 18–24% · with the suites listed as reachable above: 55–65% · with the one-line
constructor seams: 90%+.**

### Risebird.Repository — near-100% is not achievable with unit tests

About 94% of this project is repository classes that open a `SqlConnection` in place and execute
stored procedures through Dapper. `IRFRepository` alone is 566 KB. There is no injected seam, so
a unit test cannot enter those methods at all. The `Search/` helpers and `Indexing/` mappers —
the non-SQL ~6% — are now largely covered.

**Estimated now: 5–7% · achievable with unit tests alone: ~7% · with integration tests: 90%+.**

The right answer is integration tests: spin up `mcr.microsoft.com/mssql/server` with
Testcontainers, deploy the schema and stored procedures from `Risebird.DB`, and exercise the
repositories for real — which tests the thing that actually matters about a repository, that the
SQL is correct. It needs a new `Risebird.Repository.IntegrationTests` project and runs in minutes
rather than seconds. Introducing an `IDbConnectionFactory` instead would touch ~70 files and
yield tests that assert "we called this stored procedure with these parameters" rather than "the
query returns the right rows" — much lower value for much higher risk. Recommend the first.

## Suggested order of work

1. **Run `./coverage.ps1` and send the output.** Everything above becomes concrete, and the
   business sweep's ratchet message prints the exact non-defensive-method baseline to paste in.
2. **Fix the first-build compile errors.** Paste them.
3. **`VapiService` and `WhatsAppHelper` suites** — 75 KB of CommonHelper, no production change,
   and the harness for both already exists.
4. **Uncomment the three dormant `HttpClient` constructor parameters** and register them in DI.
   Cheapest coverage in the codebase.
5. **`NotificationHelper`** — its own session.
6. **Business to ~90%:** fixtures for the mid-size services, then the three giants by feature area.
7. **Decide the Repository question** — integration tests, or keep it out of the gate at ~7%.
8. **Add a CI gate** once the numbers are real: a per-assembly threshold that can ratchet up and
   cannot regress.

## A note on what "100% coverage" buys

The ratchet test in the business sweep is worth more than several percentage points of line
coverage: it enumerates every method that throws when a collaborator returns nothing, which is
precisely the production failure mode when a stored procedure returns an empty result set.
Twenty-one such latent defects are now documented across this file and
`UNIT_TESTS_PASS1_README.md`; the sweep will find the rest mechanically. Prioritising those over
the last few percent of line coverage on a well-behaved service is the better use of the effort.
