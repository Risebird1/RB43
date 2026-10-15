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
)
INSERT INTO dbo.QCActivitySetting (Name, Value, Description)
SELECT s.Name, s.Value, s.Description
FROM Seed s
WHERE NOT EXISTS (SELECT 1 FROM dbo.QCActivitySetting x WHERE x.Name = s.Name);
GO
