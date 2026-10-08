IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_GetEmailRecipientsActivityBounceDropped'))
BEGIN
    DROP PROCEDURE dbo.usp_GetEmailRecipientsActivityBounceDropped
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
-- Bounce & Dropped Mail page list.
--
-- The six per-role SELECTs were merged into one query (dbo.fn_GetBounceDroppedRows), scoped by
-- dbo.fn_BounceDroppedScopeAllows. That also fixes: #4 (unscoped query ran after the Client branch),
-- #6 (inner joins dropped interviewer bounces), #8 (CAST(CreatedAt AS date) blocked index use).
--
-- Paged mode (@PageSize set, used by the DataTable): returns
--   1) the page of rows (no MailBody; load it with usp_GetBounceMailContent)
--   2) one row: RecordsTotal (after date/scope/resolution/filter modal), RecordsFiltered (after @Search)
-- Legacy mode (@PageSize NULL): every row, with MailBody, so an app deployed before this SP keeps working.
--
-- List parameters (@SkillChannels, @SentBy, @Recipients, @Activities, @BounceReasons) are CHAR(31)-separated;
-- NULL = no filter on that field.
CREATE PROCEDURE [dbo].[usp_GetEmailRecipientsActivityBounceDropped]
@recipientType varchar(30),
@startDate datetime = null,
@endDate datetime = null,
@timeNow datetime,
@EmailId varchar(150),
@IncludeResolved bit = 0,
@SkillChannels nvarchar(max) = null,
@SentBy nvarchar(max) = null,
@Recipients nvarchar(max) = null,
@Activities nvarchar(max) = null,
@BounceReasons nvarchar(max) = null,
@Search nvarchar(200) = null,
@SortColumn varchar(50) = null,
@SortDir varchar(4) = 'desc',
@Offset int = 0,
@PageSize int = null
AS
BEGIN
SET NOCOUNT ON;

-- Same default window everywhere (bugs #2, #3): missing end = today, missing start = a 2-day window.
IF @timeNow IS NULL SET @timeNow = GETDATE();
IF @endDate IS NULL SET @endDate = @timeNow;
IF @startDate IS NULL SET @startDate = DATEADD(DAY, -1, CAST(@endDate AS date));

DECLARE @start datetime = CAST(CAST(@startDate AS date) AS datetime);
DECLARE @endExclusive datetime = DATEADD(DAY, 1, CAST(CAST(@endDate AS date) AS datetime));
DECLARE @sep char(1) = CHAR(31);

DECLARE @Role varchar(150), @ClientRoleId int, @ClientId int;
SELECT @Role = pr.Name FROM UserList ul JOIN Profile pr ON pr.id = ul.idprofile WHERE ul.EmailId = @EmailId;
IF (@Role = 'Client')
BEGIN
    SELECT TOP 1 @ClientRoleId = cu.RoleId, @ClientId = cu.ClientId
    FROM ClientUser cu JOIN UserList ul ON ul.id = cu.UserId
    WHERE ul.EmailId = @EmailId;
END

IF OBJECT_ID('tempdb..#rows') IS NOT NULL DROP TABLE #rows;

SELECT r.SendgridEventId, r.SentMailDetailsId, r.MailSubject, r.InterviewId, r.IRFId, r.SentOn, r.SentBy, r.sg_message_id,
       r.email, r.latestEvent, r.ActivityDate, r.[timestamp], r.[url], r.reason, r.[status], r.BounceReason, r.BounceCount,
       r.SkillChannelName, r.interviewDate, r.ChannelOwner, r.CandidateId, r.ResumeLink, r.MobileNumber, r.CalendlyLink,
       r.candidateEmail, r.candidateName, r.CandidateClientHR, r.EndDate, r.SchedulerUniqueid, r.InterviewerId,
       r.ResolutionStatus, r.ClientDomain, r.BounceKind
INTO #rows
FROM dbo.fn_GetBounceDroppedRows(@recipientType, @start, @endExclusive, @EmailId, @Role, @ClientRoleId, @ClientId) r
WHERE r.rn = 1
  -- Resolved / Unreachable / Resent leave the default view; EmailCorrected stays so Resend is reachable.
  AND (@IncludeResolved = 1 OR r.ResolutionStatus IS NULL OR r.ResolutionStatus = 'EmailCorrected')
  AND (@SkillChannels IS NULL OR r.SkillChannelName IN (SELECT value FROM STRING_SPLIT(@SkillChannels, @sep)))
  AND (@SentBy IS NULL OR r.SentBy IN (SELECT value FROM STRING_SPLIT(@SentBy, @sep)))
  AND (@Recipients IS NULL OR r.email IN (SELECT value FROM STRING_SPLIT(@Recipients, @sep)))
  AND (@Activities IS NULL OR r.latestEvent IN (SELECT value FROM STRING_SPLIT(@Activities, @sep)))
  AND (@BounceReasons IS NULL OR r.BounceReason IN (SELECT value FROM STRING_SPLIT(@BounceReasons, @sep)))
OPTION (RECOMPILE);

DECLARE @recordsTotal int = @@ROWCOUNT;

IF (@PageSize IS NULL)
BEGIN
    SELECT r.*, smd.MailBody
    FROM #rows r
    JOIN SentMailDetails smd ON smd.id = r.SentMailDetailsId
    ORDER BY r.SentOn DESC;

    SELECT @recordsTotal AS RecordsTotal, @recordsTotal AS RecordsFiltered;
    RETURN;
END

IF (@PageSize < 1) SET @PageSize = 10;
IF (@PageSize > 100) SET @PageSize = 100;
IF (@Offset IS NULL OR @Offset < 0) SET @Offset = 0;
SET @SortDir = CASE WHEN LOWER(@SortDir) = 'asc' THEN 'asc' ELSE 'desc' END;

DECLARE @like nvarchar(210) = NULL;
IF (LTRIM(RTRIM(ISNULL(@Search, ''))) <> '')
    SET @like = N'%' + REPLACE(REPLACE(REPLACE(LTRIM(RTRIM(@Search)), N'[', N'[[]'), N'%', N'[%]'), N'_', N'[_]') + N'%';

IF OBJECT_ID('tempdb..#filtered') IS NOT NULL DROP TABLE #filtered;

SELECT r.*
INTO #filtered
FROM #rows r
WHERE @like IS NULL
   OR r.email LIKE @like OR r.candidateName LIKE @like OR r.MailSubject LIKE @like
   OR r.SkillChannelName LIKE @like OR r.SentBy LIKE @like OR r.latestEvent LIKE @like
   OR r.BounceReason LIKE @like OR r.ChannelOwner LIKE @like OR r.MobileNumber LIKE @like
   OR r.ResolutionStatus LIKE @like;

DECLARE @recordsFiltered int = @@ROWCOUNT;

-- Whitelisted sort columns, one CASE per data type so values are never implicitly converted.
SELECT f.*
FROM #filtered f
ORDER BY
    CASE WHEN @SortDir = 'asc' THEN
        CASE @SortColumn
            WHEN 'email' THEN f.email WHEN 'candidateName' THEN f.candidateName WHEN 'MailSubject' THEN f.MailSubject
            WHEN 'SentBy' THEN f.SentBy WHEN 'latestEvent' THEN f.latestEvent WHEN 'BounceReason' THEN f.BounceReason
            WHEN 'SkillChannelName' THEN f.SkillChannelName WHEN 'ChannelOwner' THEN f.ChannelOwner
            WHEN 'MobileNumber' THEN f.MobileNumber END
    END ASC,
    CASE WHEN @SortDir = 'desc' THEN
        CASE @SortColumn
            WHEN 'email' THEN f.email WHEN 'candidateName' THEN f.candidateName WHEN 'MailSubject' THEN f.MailSubject
            WHEN 'SentBy' THEN f.SentBy WHEN 'latestEvent' THEN f.latestEvent WHEN 'BounceReason' THEN f.BounceReason
            WHEN 'SkillChannelName' THEN f.SkillChannelName WHEN 'ChannelOwner' THEN f.ChannelOwner
            WHEN 'MobileNumber' THEN f.MobileNumber END
    END DESC,
    CASE WHEN @SortDir = 'asc' THEN
        CASE @SortColumn WHEN 'SentOn' THEN f.SentOn WHEN 'ActivityDate' THEN f.ActivityDate WHEN 'interviewDate' THEN f.interviewDate END
    END ASC,
    CASE WHEN @SortDir = 'desc' THEN
        CASE @SortColumn WHEN 'SentOn' THEN f.SentOn WHEN 'ActivityDate' THEN f.ActivityDate WHEN 'interviewDate' THEN f.interviewDate END
    END DESC,
    CASE WHEN @SortDir = 'asc' AND @SortColumn = 'BounceCount' THEN f.BounceCount END ASC,
    CASE WHEN @SortDir = 'desc' AND @SortColumn = 'BounceCount' THEN f.BounceCount END DESC,
    f.SentOn DESC, f.SendgridEventId DESC
OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;

SELECT @recordsTotal AS RecordsTotal, @recordsFiltered AS RecordsFiltered;
END
GO
