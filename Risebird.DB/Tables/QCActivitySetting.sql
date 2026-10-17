/*
================================================================================
QCActivitySetting — thresholds for QC Activity authenticity rules and flags, so the
QC lead can tune them without a deploy. One row per setting (Name = key).

  MinCallSeconds    120   a connected coaching call lasts at least this long
  MinCommentChars    20   an authentic action has a comment at least this long ...
  StockComments     ok,done,fine,checked,...   ... that is not one of these (comma list)
  BurstCount         10   Burst flag: this many actions of the same type by one person ...
  BurstMinutes       10   ... within this many minutes
  ReversalDays        7   Reversed-by-another-QC flag: undone by a different person within N days
  ShortCallSeconds   30   Short-calls flag: dialled calls under N seconds ...
  ShortCallPct       50   ... make up more than X% of the person's dialled calls
  PliSelectCommentChars 500  PLI short comment on a Selected report (QC Tracking Delta 5) ...
  PliRejectCommentChars 250  ... and on a Rejected report
  PliMinQuestions        5   fewer questions than this (0 included) is flagged
  PliLateJoinMinutes     5   joining later than this is a late join
  PqiSelectPct          40   high select rate ...
  PqiRejectPct          80   ... high reject rate ...
  PqiMinInterviews       5   ... judged only with at least this many completed interviews
  PqiNegativeRating      4   candidate rating below this is negative

Read by usp_QCActivity_CollectRows (authenticity) and usp_GetQCActivityFlags.
A missing row falls back to the value above (coded in both procedures).
Idempotent: creates the table and inserts only the missing rows.
================================================================================
*/
IF OBJECT_ID(N'dbo.QCActivitySetting', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.QCActivitySetting
    (
        Name          VARCHAR(50)    NOT NULL,
        Value         NVARCHAR(1000) NOT NULL,
        Description   NVARCHAR(300)  NULL,
        ModifiedBy    VARCHAR(150)   NULL,
        ModifiedAtUtc DATETIME       NOT NULL CONSTRAINT DF_QCActivitySetting_ModifiedAt DEFAULT (GETUTCDATE()),
        CONSTRAINT PK_QCActivitySetting PRIMARY KEY CLUSTERED (Name ASC)
    );
END
GO

;WITH Seed (Name, Value, Description) AS (
    SELECT 'MinCallSeconds',   N'120', N'Connected coaching call: answered, to an interviewer, at least this many seconds'
    UNION ALL SELECT 'MinCommentChars', N'20', N'Authentic action: comment at least this many characters'
    UNION ALL SELECT 'StockComments', N'ok,okay,done,fine,checked,check,na,n/a,nil,none,reviewed,good,noted,yes,no,-,.', N'Comments that do not count as a real comment (comma-separated, case-insensitive)'
    UNION ALL SELECT 'BurstCount', N'10', N'Burst flag: this many actions of the same type by one person ...'
    UNION ALL SELECT 'BurstMinutes', N'10', N'... within this many minutes'
    UNION ALL SELECT 'ReversalDays', N'7', N'Reversed by another QC: undone by a different person within this many days'
    UNION ALL SELECT 'ShortCallSeconds', N'30', N'Short calls flag: dialled calls under this many seconds ...'
    UNION ALL SELECT 'ShortCallPct', N'50', N'... are more than this % of the person''s dialled calls'
    -- QC Tracking Delta 5: PLI / PQI process signals (usp_InterviewerProcessSignals_Collect, usp_GetInterviewerProcessSignals)
    UNION ALL SELECT 'PliSelectCommentChars', N'500', N'PLI: a Selected report with a comment under this many characters is a short comment'
    UNION ALL SELECT 'PliRejectCommentChars', N'250', N'PLI: a Rejected report with a comment under this many characters is a short comment'
    UNION ALL SELECT 'PliMinQuestions', N'5', N'PLI: an interview with fewer questions discussed than this (0 included) is flagged'
    UNION ALL SELECT 'PliLateJoinMinutes', N'5', N'PLI: joining more than this many minutes after the start is a late join'
    UNION ALL SELECT 'PqiSelectPct', N'40', N'PQI: select rate at or above this % is high (with the minimum volume below)'
    UNION ALL SELECT 'PqiRejectPct', N'80', N'PQI: reject rate at or above this % is high (with the minimum volume below)'
    UNION ALL SELECT 'PqiMinInterviews', N'5', N'PQI: select / reject rates are judged only with at least this many completed interviews'
    UNION ALL SELECT 'PqiNegativeRating', N'4', N'PQI: a candidate rating below this is negative'
)
INSERT INTO dbo.QCActivitySetting (Name, Value, Description)
SELECT s.Name, s.Value, s.Description
FROM Seed s
WHERE NOT EXISTS (SELECT 1 FROM dbo.QCActivitySetting x WHERE x.Name = s.Name);
GO
