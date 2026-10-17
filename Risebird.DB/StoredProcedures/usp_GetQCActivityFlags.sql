/*
================================================================================
usp_GetQCActivityFlags — QC Activity Flags panel (part 2: authentic QC work).

Collects every QC row in the IST window (all people, so calls and coaching by
anyone count for "reviewed with no call"), computes the flags with
usp_QCActivity_ComputeFlags, and returns them for one person (@ActorEmail) or
everyone (NULL).

Result sets:
  1 Flags     Flag, ActorEmail, Count, FirstAt, LastAt, Detail, Metrics (metric
              keys for the existing drill-down)
  2 Items     the rows behind each flag (top 3000): Flag, ActorEmail, ActionAtIst,
              Metric, InterviewId, IrfId, IrfName, InterviewerEmail,
              InterviewerName, Detail, SourceId, Note
  3 Settings  Name, Value from dbo.QCActivitySetting (the thresholds in force)

  -- Flags for one person, last 7 days
  EXEC dbo.usp_GetQCActivityFlags @FromIst = '2026-10-08', @ToIst = '2026-10-14', @ActorEmail = 'qc.c@example.com';

Deploy after usp_QCActivity_CollectRows and usp_QCActivity_ComputeFlags.
================================================================================
*/
CREATE OR ALTER PROCEDURE [dbo].[usp_GetQCActivityFlags]
    @FromIst    DATE,
    @ToIst      DATE,
    @ActorEmail VARCHAR(150) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SET @ActorEmail = NULLIF(LTRIM(RTRIM(@ActorEmail)), '');

    CREATE TABLE #QCActivityRows (
        ActorEmail        VARCHAR(150)  NOT NULL,
        Metric            VARCHAR(30)   NOT NULL,
        ActionAtIst       DATETIME      NULL,
        InterviewId       INT           NULL,
        IrfId             INT           NULL,
        InterviewerUserId INT           NULL,
        InterviewerEmail  VARCHAR(200)  NULL,
        Amount            INT           NULL,
        Detail            NVARCHAR(500) NULL,
        [Status]          VARCHAR(150)  NULL,
        Phone             VARCHAR(30)   NULL,
        RecordingUrl      VARCHAR(1000) NULL,
        SourceId          INT           NULL,
        RefId             INT           NULL,
        Comment           NVARCHAR(500) NULL,
        HasEvidence       BIT           NULL,
        IsAuthentic       BIT           NULL,
        PageGroup         VARCHAR(30)   NULL
    );

    EXEC dbo.usp_QCActivity_CollectRows
        @FromIst    = @FromIst,
        @ToIst      = @ToIst,
        @ActorEmail = NULL,
        @Metric     = NULL;

    CREATE TABLE #QCActivityFlags (
        Flag        VARCHAR(30)   NOT NULL,
        ActorEmail  VARCHAR(150)  NOT NULL,
        [Count]     INT           NOT NULL,
        FirstAt     DATETIME      NULL,
        LastAt      DATETIME      NULL,
        Detail      NVARCHAR(500) NULL,
        Metrics     VARCHAR(200)  NULL
    );
    CREATE TABLE #QCActivityFlagItems (
        Flag              VARCHAR(30)   NOT NULL,
        ActorEmail        VARCHAR(150)  NOT NULL,
        ActionAtIst       DATETIME      NULL,
        Metric            VARCHAR(30)   NOT NULL,
        InterviewId       INT           NULL,
        IrfId             INT           NULL,
        InterviewerUserId INT           NULL,
        InterviewerEmail  VARCHAR(200)  NULL,
        Detail            NVARCHAR(500) NULL,
        SourceId          INT           NULL,
        Note              NVARCHAR(300) NULL
    );

    EXEC dbo.usp_QCActivity_ComputeFlags @FromIst = @FromIst, @ToIst = @ToIst;

    /* 1 Flags */
    SELECT f.Flag, f.ActorEmail, f.[Count], f.FirstAt, f.LastAt, f.Detail, f.Metrics
    FROM #QCActivityFlags f
    WHERE @ActorEmail IS NULL OR f.ActorEmail = @ActorEmail
    ORDER BY f.ActorEmail,
             CASE f.Flag WHEN 'KeepDespiteP0' THEN 0 WHEN 'ReversedByOther' THEN 1 WHEN 'ReviewedNoCall' THEN 2
                         WHEN 'Burst' THEN 3 WHEN 'ShortCalls' THEN 4 ELSE 5 END;

    /* 2 Items */
    SELECT TOP (3000)
        i.Flag, i.ActorEmail, i.ActionAtIst, i.Metric, i.InterviewId, i.IrfId, irf.IRFId AS IrfName,
        COALESCE(u.EmailId, i.InterviewerEmail) AS InterviewerEmail,
        u.FullName AS InterviewerName,
        i.Detail, i.SourceId, i.Note
    FROM #QCActivityFlagItems i
    LEFT JOIN dbo.IRF irf ON irf.id = i.IrfId
    OUTER APPLY (
        SELECT TOP 1 ul.EmailId,
               NULLIF(LTRIM(RTRIM(ISNULL(ul.FirstName, '') + ' ' + ISNULL(ul.LastName, ''))), '') AS FullName
        FROM dbo.UserList ul
        WHERE (i.InterviewerUserId IS NOT NULL AND ul.id = i.InterviewerUserId)
           OR (i.InterviewerUserId IS NULL AND ul.EmailId = i.InterviewerEmail)
        ORDER BY ul.id DESC
    ) u
    WHERE @ActorEmail IS NULL OR i.ActorEmail = @ActorEmail
    ORDER BY i.ActorEmail, i.Flag, i.ActionAtIst DESC;

    /* 3 Settings */
    IF OBJECT_ID(N'dbo.QCActivitySetting', N'U') IS NOT NULL
        EXEC sp_executesql N'SELECT Name, Value FROM dbo.QCActivitySetting ORDER BY Name';
    ELSE
        SELECT CAST(NULL AS VARCHAR(50)) AS Name, CAST(NULL AS NVARCHAR(1000)) AS Value WHERE 1 = 0;

    DROP TABLE IF EXISTS #QCActivityFlagItems;
    DROP TABLE IF EXISTS #QCActivityFlags;
    DROP TABLE IF EXISTS #QCActivityRows;
END
GO
