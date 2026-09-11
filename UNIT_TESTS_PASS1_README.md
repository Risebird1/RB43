# Unit test pass 1 — what landed, how to run it, what's left

**Scope agreed:** business services + CommonHelper pure logic + repository layer;
depth-first on the highest-risk types. ~1,440 executable test cases across 30 files
(625 `[Fact]` + 176 `[Theory]` with 817 inline rows).

## New / changed files

### New project — `Risebird.CommonHelper.Tests`
Deterministic helpers, no I/O. Also covers the pure-logic helper family that lives in
`Risebird.Entities/ViewModel` (referenced by Business, so it belongs in this pass).

| Area | File | Cases |
|---|---|---|
| Phone sanitisation (E.164, trunk zero, 6–9 rule) | `Helpers/PhoneNumberHelperTests.cs` | 52 |
| Email validation | `Helpers/ValidationHelperTests.cs` | 23 |
| IST conversion + display formatting | `Helpers/IndiaTimeHelperTests.cs` | 19 |
| Candidate coded-ID generation, collisions, GUID fallback | `Helpers/CandidatePseudonymHelperTests.cs` | 34 |
| Resume upload extension/MIME allow- and block-lists | `Helpers/ResumeUploadValidatorTests.cs` | 56 |
| Proctoring risk scoring + clamping | `Helpers/ProctoringRiskScorerTests.cs` | 31 |
| Enum descriptions, job-level bands | `Helpers/EnumAndJobLevelHelperTests.cs` | 13 |
| AES round-trips and every failure sentinel | `Helpers/EncryptionHelperTests.cs` | 61 |
| ES256 launch-token signing, expiry, dev-key fallback | `Helpers/ProctoringLaunchTokenServiceTests.cs` | 25 |
| Auto-QC verdict resolution + audit constants | `EntitiesHelpers/DmIvAutoQcAuditConstantsTests.cs` | 61 |
| Active/vet/record/surcharge/coded-CV state rules | `EntitiesHelpers/DmIvStateHelperTests.cs` | 112 |
| Interview-cap decision tree (all five branches) | `EntitiesHelpers/DmIvCapHelperTests.cs` | 57 |
| RB/Client vet display + stored resolution | `EntitiesHelpers/DmIvVetDisplayHelperTests.cs` | 49 |
| Rubric prompt building + skill-name matching | `EntitiesHelpers/DmIvQuestionRubricHelperTests.cs` | 40 |
| Trial priority tiers + evidence-badge filtering | `EntitiesHelpers/TrialFlagHelperTests.cs` | 103 |
| Payment-deduction + QC-verdict display | `EntitiesHelpers/StatsDetailsHelperTests.cs` | 45 |
| Bank/PAN change detection | `EntitiesHelpers/VerificationHelperTests.cs` | 43 |
| Proctoring enum wire encoding + parsing | `EntitiesHelpers/ProctoringEnumExtensionsTests.cs` | 44 |
| QC merge JSON parsing (fences, legacy array, garbage) | `EntitiesHelpers/QcInterviewAIMergeAssistantResponseTests.cs` | 27 |

Support: `Infrastructure/TestConfiguration.cs` (in-memory `IConfiguration`),
`Infrastructure/FakeFormFile.cs` (in-memory `IFormFile`).

### New project — `Risebird.Repository.Tests`
The genuinely unit-testable slice of the data layer: OData filter building, typed
field reading, options binding, and index document mapping.

| Area | File | Cases |
|---|---|---|
| OData escaping / date formatting / `IN` filters (incl. injection) | `Search/AzureSearchODataTests.cs` | 40 |
| Typed reads from Azure Search documents | `Search/AzureSearchFieldReaderTests.cs` | 43 |
| Per-listing options binding + defaults | `Search/ListingSearchOptionsLoaderTests.cs` | 23 |
| SQL row → domain doc → Azure doc → list VM round trip | `Indexing/InterviewerIndexDocumentMapperTests.cs` | 40 |

### Extended — `Risebird.Business.Tests`

| Area | File | Cases |
|---|---|---|
| DesktopProctor session lifecycle, TTL, dedup, risk delta | `ProctoringTests/ProctoringApiServiceTests.cs` + `Fixtures/ProctoringApiServiceFixture.cs` | 57 |
| KYC payout lapse: reminder schedule, cap, remarks, ordering | `FinanceTests/KycPayoutLapseServiceTests.cs` | 44 |
| TDS quarter resolution, FY select lists, reminder schedule | `FinanceTests/TdsComplianceServiceTests.cs` | 58 |
| Trial judgment: every factor boundary, severity, backfill | `TrialTests/TrialJudgmentServiceTests.cs` | 92 |
| Backfill mass-fire circuit breaker | `TrialTests/RiskCategoryMassFireGuardTests.cs` | 34 |
| Auto-QC flag: dedup, escalation, badge, audit metadata | `AutoQcTests/DmIvAutoQcFlagServiceTests.cs` | 43 |
| Seven thin services: delegation + swallow-and-log fallbacks | `ThinServiceTests/DelegatingServiceTests.cs` | 73 |

### Production files changed (two lines of build config, no logic)

- `Risebird.Business.csproj` — adds `<InternalsVisibleTo Include="Risebird.Business.Tests" />`
  so `TrialJudgmentService.EvaluateFactors` / `ApplyRiskVerdictAndSeverity` (both `internal static`)
  can be tested directly instead of only through the orchestration path.
- `Risebird.Repository.csproj` — adds `<InternalsVisibleTo Include="Risebird.Repository.Tests" />`
  for the `internal` index mappers, field-name constants and `DapperStoredProcedureHelper`.
- `Risebird.Business.Tests.csproj` — adds `Microsoft.Extensions.Configuration` and a
  `Microsoft.AspNetCore.App` framework reference (needed for `SelectListItem` and `IFormFile`).
- `RisebirdPortal.sln` — registers the two new test projects with Debug/Release configs.

## How to run

```
dotnet build RisebirdPortal.sln
dotnet test Risebird.CommonHelper.Tests/Risebird.CommonHelper.Tests.csproj
dotnet test Risebird.Repository.Tests/Risebird.Repository.Tests.csproj
dotnet test Risebird.Business.Tests/Risebird.Business.Tests.csproj
```

Or everything plus coverage:

```
dotnet test RisebirdPortal.sln --collect:"XPlat Code Coverage"
```

These were written against the source but **not compiled** — there is no .NET SDK in the
session that produced them. Expect a handful of signature mismatches on the first build.
Paste the compiler / test output back and I'll fix them.

## Behaviours these tests deliberately pin (worth a look — several are latent bugs)

1. **`CandidatePseudonymHelper.BuildNickName(_, _, int.MinValue, _)` throws `OverflowException`**
   — `Math.Abs(int.MinValue)` cannot be negated. Harmless today (identity columns never
   reach it) but it is a real unguarded path.
2. **`EncryptionHelper.DecryptIntegerValue` returns `0` both for a decrypted id of 0 and for
   any failure** — callers cannot distinguish "id 0" from "tampered token".
3. **`EncryptionHelper.EncryptIntegerValue` / `EncryptStringValue` replace `+` with `_` but leave
   `/` and `=`** — the output is not fully URL-safe; callers still have to URL-encode.
4. **`EncryptionHelper`'s constructor throws (NRE) when `EncryptionDetails:Key` is missing** rather
   than failing with a clear configuration error.
5. **`ValidationHelper.ValidateEmailAddress` catches only `FormatException`.** `MailAddress` can
   throw `ArgumentException` for some inputs, which would escape. It also accepts RFC-822
   display-name form (`Ada Lovelace <ada@risebird.io>`) as valid.
6. **`IndiaTimeHelper.ToUtcIso8601` relabels a `Local` `DateTime` as UTC instead of converting it** —
   `SpecifyKind` does not shift the clock, so a local value is reported an offset out.
7. **`ResumeUploadValidator` accepts a filename that is nothing but an extension** (`.pdf`), because
   `Path.GetExtension(".pdf")` returns `".pdf"`. It also trusts the validated extension whenever the
   sniffer returns an unrecognised-but-not-blocked MIME type (deliberate, now documented).
8. **`DmIvCapHelper.GetEffectiveCap` returns 0 under a free policy** — rendering it without first
   checking `ShowUnlimited` / `IsCapEnforced` shows "0 of 0" for an uncapped channel.
9. **`GeneralSkill` / `Joinability` / `Selectability` matrix services call the repository twice per
   `GetById`** (the null-guard evaluates the `Task`, then the value is fetched again), and their
   `Delete` methods return `true` without awaiting the repository — a rejected delete reports success.
10. **`ProctoringApiService.MintSessionAsync` trusts every request** (candidate/interview validation is
    switched off by a product decision) and registers the session as `Verified`. Pinned so re-enabling
    validation is a deliberate change.
11. **`ProctoringApiService.GetLogsForCandidateAsync` does not null-guard the repository result** — a
    null return throws instead of yielding an empty list.
12. **`AzureSearchOData.Escape` is not idempotent** — escaping an already-escaped literal doubles the
    quotes again.
13. **`AzureSearchFieldReader.GetInt` / `GetBool` / `GetDateTime` throw on an unparseable value** rather
    than returning the zero-ish default, unlike their missing-field behaviour.
14. **`KycPayoutLapseService.ProcessLapseCandidatesAsync` has no per-row try/catch** — one failing insert
    abandons the whole batch and reports 0 added.
15. **`TdsComplianceService.GetDueReminderIndex` has no cap** while the KYC equivalent caps at 5. If the
    two are meant to behave alike, one of them is wrong.

## What is NOT covered yet (candidates for pass 2)

- The three giant business services — `IRFService` (262 KB), `MeetingService` (234 KB),
  `InterviewerService` (111 KB, partially covered by the existing suite). These need the
  reflection-driven smoke pattern plus targeted suites per feature area.
- `WalletService`, `CashfreePayoutService`, `DmIvAutoQcRemovalService`, `DmIvAutoQcBackfillService`,
  `MCQService`, `InterviewSchedulerService`, `CandidateService`, `ClientService`, `UserService`,
  `DashboardService`.
- `TdsComplianceService.GetComplianceAsync` / `BuildExport26QAsync` / `ProcessForm16DumpAsync` —
  these need a fully stubbed `ITdsRepository` and EPPlus assertions; worth their own pass.
- CommonHelper types that do real network I/O and would need a refactor (an injectable
  `HttpMessageHandler`) before they can be unit tested: `VmHelper`, `NotificationHelper`,
  `WhatsAppHelper`, `AssistantServiceHelper`, `TwilioHelper`, `VapiService`, `AzureStorageHelper`,
  `OpenAI*`, `Zoom*`/`Teams*`/`Zoho*`/`Calendly*` helpers, `SASUrlHelper`, `PasswordHelper`
  (needs `IB2CClientService` + a seeded `Random` to be deterministic).
- The remaining index mappers — `Irf`, `CallCatalogue`, `Template`, `RaisedIssue` — follow the exact
  shape of the interviewer mapper suite and are mostly copy-adapt work.
- The repository classes themselves (`IRFRepository` 566 KB, `MeetingRespository` 414 KB, …) are raw
  Dapper against SQL Server. They need **integration** tests against a LocalDB/Testcontainers instance,
  not unit tests; `DapperStoredProcedureHelper` is now reachable from the test project but only
  exercises real connections.
- The Web layer (70 controllers) — excluded from this pass by choice.
