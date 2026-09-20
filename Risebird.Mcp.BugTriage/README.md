# Risebird Bug Triage MCP Server

A read-only MCP server that gives a coding agent the context it needs to diagnose a Risebird bug:
production telemetry, the work item or issue that reported it, and the source file the stack trace
points at.

It can also apply a fix — but only a small, reviewed, reversible one, and only with explicit consent.
Writing is **off by default**; see [Applying fixes](#applying-fixes). Nothing here commits, pushes,
opens a PR, or updates a work item.

## Why it exists

Minor bug fixes are rarely hard because the fix is hard. They are slow because the context is
scattered: the exception is in Application Insights, the report is in Azure DevOps or a support
email, and the stack trace points at a build-agent path that means nothing locally. This server
collapses that into a few tool calls.

## Tools

### Telemetry — Application Insights (read-only KQL)

| Tool | Use it when |
|---|---|
| `query_exceptions` | "Something is erroring in production." Groups exceptions by type and message; returns a `problemId` and sample `operationId`. |
| `get_exception_detail` | You have a `problemId` or `operationId` and want the full stack trace and custom dimensions. |
| `get_operation_timeline` | The single most useful one. Every request, dependency, trace and exception for one `operation_Id`, in order — shows what happened *before* the failure. |
| `query_failed_requests` | "Page X is broken" / "500s on the portal", with no named exception. |
| `run_kql` | Escape hatch for a query the above do not cover. |

`run_kql` rejects `externaldata`, `evaluate http_request`, `.ingest`, `.set`, `.append`, `.alter`,
`.drop` and `.create`. The Logs query API is read-only regardless; the guard additionally stops a
query being used to read from or push data to somewhere outside the workspace.

### Issue sources

| Tool | Source |
|---|---|
| `get_work_item`, `search_work_items` | Azure DevOps (WIQL; only `SELECT` accepted) |
| `get_github_issue`, `search_github_issues` | GitHub issues and PRs |
| `analyze_bug_report` | Free-form human text — a run comment, support email, Teams message |

`analyze_bug_report` is deterministic pattern matching, not inference. It pulls exception types,
stack frames, file and component names, URLs, `operation_Id`s, GUIDs, HTTP status codes and work
item references out of prose, then suggests which of the other tools to call next. It deliberately
does not diagnose — that is the agent's job, on real evidence.

### Source correlation

| Tool | Use it when |
|---|---|
| `resolve_stack_trace` | You have a stack trace. Maps every frame to a repo-relative file and line, with surrounding source and recent commits. |
| `get_file_history` | "Did this break recently?" |
| `read_source_window` | Widen the context around a line. |

`resolve_stack_trace` handles the build-agent path problem: a frame recorded as
`D:\a\1\s\Risebird.Business\UserService.cs:line 40` resolves to `Risebird.Business\UserService.cs`
in your checkout by matching the longest trailing path segment that exists locally. Frames with no
recorded path fall back to matching the declaring type's short name, and report ambiguity rather
than guessing when several files match.

## Applying fixes

Claude Code can already edit files directly. These tools exist only for what plain editing does not
give you: a reviewed diff before anything moves, a hash-guarded atomic write, an automatic build
check, and a one-call undo.

| Tool | |
|---|---|
| `propose_fix` | Read-only. Validates the change and returns a diff and a `proposalId`. Writes nothing. |
| `apply_fix` | Writes a proposal after consent. Builds the project afterwards and reverts automatically if the build breaks. |
| `revert_fix` | Restores the file exactly as it was before a fix. |
| `verify_build` | Builds the project owning a file and reports compiler errors. |
| `list_fixes` | Pending proposals and fixes applied this session. |

### Consent

Three independent gates, so no single failure applies a change on its own:

1. **The client's own approval prompt.** `apply_fix` and `revert_fix` are annotated
   `destructive`/`not read-only`, so Claude Code asks before calling them.
2. **The server asks the user directly**, via MCP elicitation, naming the file, the branch, the
   rationale and the diff. The user must type `APPLY`; anything else cancels.
3. **A two-phase handshake.** `apply_fix` needs a `proposalId` from a prior `propose_fix`, so the
   change was described and diffed in a separate call before it could be written.

If the client does not support elicitation, gate 2 falls back to an explicit `confirm="APPLY"`
argument — which the agent is instructed to set only after showing the user the diff and getting a
yes. It is a weaker gate than a real prompt, and the result says which gate was used
(`consentVia`).

### Rails that consent cannot waive

- **Off by default** — `Triage:Fix:Enabled` must be set to `true`. `propose_fix` still works while
  disabled, so the analysis workflow is unaffected.
- **Inside the checkout only** — paths are normalised and containment-checked; `..\` cannot escape.
- **Existing source files only** — allowed extensions only (`.cs`, `.cshtml`, `.razor`, `.js`,
  `.css`, `.sql`, `.md`), never `bin`/`obj`, and never creating a new file.
- **Never configuration or secrets** — `appsettings*.json`, `local.settings.json`, `*.pubxml`,
  `*.arm.json`, `web.config` and friends are blocked by name as a second layer, so they stay blocked
  even if the allowed-extension list is widened later.
- **Never a protected branch** — `master`, `main`, `dev2026`, `release`. When git is unreachable the
  branch cannot be checked, and the result says so explicitly rather than assuming it is safe.
- **Unique, bounded, unchanged** — the snippet must appear exactly once, the change must be under
  `MaxChangedLines` (120), and the file's hash must match what it was at proposal time. The hash is
  re-checked immediately before writing, so a file edited while the user was deciding is not clobbered.
- **Verified, not assumed** — the owning project is built after the write, and a failing build is
  rolled back automatically.

A clean build is not a fixed bug. The tool says so in its own result, and tests and human review
still apply.

## Configuration

`appsettings.json` holds non-secret settings. **Secrets never go in it** — use user-secrets locally
or environment variables on a build agent.

```bash
dotnet user-secrets set "Triage:AzureDevOps:PersonalAccessToken" "<pat>" --project Risebird.Mcp.BugTriage
```

```bash
dotnet user-secrets set "Triage:GitHub:Token" "<token>" --project Risebird.Mcp.BugTriage
```

Environment variables use the `RISEBIRD_TRIAGE_` prefix with `__` for nesting, e.g.
`RISEBIRD_TRIAGE_Triage__GitHub__Token`.

| Setting | Notes |
|---|---|
| `Triage:AppInsights:ResourceId` | Preferred. Full ARM resource id of the App Insights component — keeps the classic table names (`exceptions`, `requests`, …). |
| `Triage:AppInsights:WorkspaceId` | Log Analytics workspace GUID, used only if `ResourceId` is unset. |
| `Triage:AppInsights:DefaultLookbackHours` | Default 24. |
| `Triage:AppInsights:MaxRows` | Default 50. Caps every tool's result size so the agent gets readable output. |
| `Triage:AzureDevOps:Organization` / `:Project` | Organisation name only, not the full URL. |
| `Triage:AzureDevOps:PersonalAccessToken` | **Work Items: Read** scope only. Secret. |
| `Triage:GitHub:DefaultRepository` | `owner/name`, so callers can omit it. |
| `Triage:GitHub:Token` | Fine-grained, read-only Issues + Pull requests. Secret. |
| `Triage:Repository:RootPath` | Absolute path to the checkout stack traces map onto. |
| `Triage:Repository:GitExecutable` | Only needed if `git.exe` is not on `PATH` or in the usual install locations. |
| `Triage:Fix:Enabled` | **Default `false`.** Must be `true` before `apply_fix` will write anything. |
| `Triage:Fix:ProtectedBranches` | Default `master`, `main`, `dev2026`, `release`. |
| `Triage:Fix:MaxChangedLines` | Default 120. Upper bound on a single fix. |
| `Triage:Fix:ProposalLifetimeMinutes` | Default 30. How long an unapplied proposal stays valid. |

App Insights uses `DefaultAzureCredential` — your `az login` / Visual Studio account locally, a
managed identity on a build agent. The identity needs only the **Monitoring Reader** role on the
component. There is no App Insights secret to store.

Each source is independent: configure only the ones you want. An unconfigured source's tools return
an explanatory message naming the missing setting rather than failing obscurely.

## Registering with Claude Code

Build first (`dotnet build Risebird.Mcp.BugTriage`), then add to `.mcp.json` in the repository root:

```json
{
  "mcpServers": {
    "risebird-bug-triage": {
      "command": "C:\\Users\\premvm\\source\\repos\\Risebird\\Risebird.Mcp.BugTriage\\bin\\Debug\\net10.0\\Risebird.Mcp.BugTriage.exe"
    }
  }
}
```

For a stable install, `dotnet publish -c Release` and point `command` at the published executable
instead of the Debug build.

## A typical triage

1. Someone pastes a complaint into a run comment. → `analyze_bug_report`
2. It finds an `operation_Id`. → `get_operation_timeline` shows the failing request and the
   dependency call that returned 500 just before it.
3. The exception has a stack trace. → `resolve_stack_trace` gives the file, the line, the
   surrounding source, and that the file was last touched two days ago.
4. The agent now has real evidence and proposes a fix. You review and apply it.

## Implementation notes

- **stdio transport**: all logging is forced to stderr. Anything written to stdout corrupts the
  JSON-RPC frame and the client silently drops the connection.
- **Content root** is pinned to `AppContext.BaseDirectory` — an MCP client launches the process with
  an arbitrary working directory, so the default (current directory) would silently miss
  `appsettings.json`.
- **Package versions**: the MCP SDK requires the `10.0.10` servicing band of `Microsoft.Extensions.*`
  while the solution pins `10.0.0` centrally. This project sets
  `CentralPackageTransitivePinningEnabled=false` and uses `VersionOverride` on its direct references
  so the rest of the solution is untouched. If the solution is ever moved to `10.0.10`, both can be
  removed.
