-- The portal's read-through cache of one AI interview's MCQ section result.
--
-- Why a cache and not replication
-- -------------------------------
-- The MCQ test runs on screen.risebird.io against its own database, and stays there. Three ways
-- to get its result onto AIInterview/Report.cshtml were considered:
--
--   1. Copy the MCQ tables across (linked server, or a sync job). Rejected: it duplicates a
--      schema this database does not own and cannot migrate, and every MCQ-side change becomes a
--      breaking change here. It also copies far more than the report needs.
--   2. Fetch live on every render, store nothing. Rejected on its own: the report is also
--      rendered headlessly into a PDF and is read by clients long after the interview, so a
--      transient failure or a retired session on the MCQ side would produce a report with a hole
--      in it - and the same report would not render the same way twice.
--   3. Fetch live, and keep the last good response. Chosen.
--
-- So: the MCQ database remains the source of truth and is never bulk-copied. The portal asks for
-- one session's report over HTTP when it needs it, and writes what came back here. A later render
-- prefers a fresh fetch and falls back to this row, which means the report keeps working when the
-- MCQ side is unreachable, and a PDF generated months later shows what the report showed.
--
-- ReportJson holds the whole response verbatim, so adding a field on the MCQ side needs no
-- migration here. The columns beside it are denormalised purely so the portal can list, sort and
-- filter candidates on their MCQ score without unpacking JSON in SQL.
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_NAME = N'AIInterviewMcqAssessment')
BEGIN
CREATE TABLE [dbo].[AIInterviewMcqAssessment](
    [id] [int] IDENTITY(1,1) NOT NULL,
    [SessionId] [varchar](50) NOT NULL,
    [InterviewId] [int] NULL,
    [IRFId] [int] NULL,
    [McqId] [varchar](50) NULL,
    [Status] [varchar](20) NULL,
    [TotalQuestions] [int] NULL,
    [CorrectAnswers] [int] NULL,
    [ScorePercent] [decimal](5,2) NULL,
    [RedFlags] [int] NULL,
    [AllottedMinutes] [int] NULL,
    [ElapsedSeconds] [int] NULL,
    [TabSwitchCount] [int] NULL,
    [StartedOn] [datetime] NULL,
    [CompletedOn] [datetime] NULL,
    [ReportJson] [nvarchar](max) NULL,
    [FetchedOn] [datetime] NULL,
 CONSTRAINT [PK_AIInterviewMcqAssessment] PRIMARY KEY CLUSTERED
(
    [id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
END
GO

-- One cached result per interview session; the upsert depends on this being unique.
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = N'UX_AIInterviewMcqAssessment_SessionId'
    AND object_id = OBJECT_ID(N'dbo.AIInterviewMcqAssessment'))
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX [UX_AIInterviewMcqAssessment_SessionId]
    ON [dbo].[AIInterviewMcqAssessment] ([SessionId] ASC)
END
GO
