IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_GetBounceDroppedFilterOptions'))
BEGIN
    DROP PROCEDURE dbo.usp_GetBounceDroppedFilterOptions
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
-- Filter-modal options of the Bounce & Dropped Mail page for the chosen date range, built from the
-- same rows as the list (dbo.fn_GetBounceDroppedRows) rather than from the rows on screen, so a filter
-- can always be widened again. Returns (FilterName, FilterValue) pairs:
-- SkillChannel | SentBy | Recipient | Activity | BounceReason.
CREATE PROCEDURE [dbo].[usp_GetBounceDroppedFilterOptions]
@recipientType varchar(30),
@startDate datetime = null,
@endDate datetime = null,
@timeNow datetime,
@EmailId varchar(150),
@IncludeResolved bit = 0
AS
BEGIN
SET NOCOUNT ON;

IF @timeNow IS NULL SET @timeNow = GETDATE();
IF @endDate IS NULL SET @endDate = @timeNow;
IF @startDate IS NULL SET @startDate = DATEADD(DAY, -1, CAST(@endDate AS date));

DECLARE @start datetime = CAST(CAST(@startDate AS date) AS datetime);
DECLARE @endExclusive datetime = DATEADD(DAY, 1, CAST(CAST(@endDate AS date) AS datetime));

DECLARE @Role varchar(150), @ClientRoleId int, @ClientId int;
SELECT @Role = pr.Name FROM UserList ul JOIN Profile pr ON pr.id = ul.idprofile WHERE ul.EmailId = @EmailId;
IF (@Role = 'Client')
BEGIN
    SELECT TOP 1 @ClientRoleId = cu.RoleId, @ClientId = cu.ClientId
    FROM ClientUser cu JOIN UserList ul ON ul.id = cu.UserId
    WHERE ul.EmailId = @EmailId;
END

IF OBJECT_ID('tempdb..#opt') IS NOT NULL DROP TABLE #opt;

SELECT r.SkillChannelName, r.SentBy, r.email, r.latestEvent, r.BounceReason
INTO #opt
FROM dbo.fn_GetBounceDroppedRows(@recipientType, @start, @endExclusive, @EmailId, @Role, @ClientRoleId, @ClientId) r
WHERE r.rn = 1
  AND (@IncludeResolved = 1 OR r.ResolutionStatus IS NULL OR r.ResolutionStatus = 'EmailCorrected')
OPTION (RECOMPILE);

SELECT FilterName, FilterValue
FROM (
    SELECT DISTINCT 'SkillChannel' AS FilterName, CAST(SkillChannelName AS nvarchar(500)) AS FilterValue FROM #opt WHERE ISNULL(SkillChannelName, '') <> ''
    UNION
    SELECT DISTINCT 'SentBy', CAST(SentBy AS nvarchar(500)) FROM #opt WHERE ISNULL(SentBy, '') <> ''
    UNION
    SELECT DISTINCT 'Recipient', CAST(email AS nvarchar(500)) FROM #opt WHERE ISNULL(email, '') <> ''
    UNION
    SELECT DISTINCT 'Activity', CAST(latestEvent AS nvarchar(500)) FROM #opt WHERE ISNULL(latestEvent, '') <> ''
    UNION
    SELECT DISTINCT 'BounceReason', CAST(BounceReason AS nvarchar(500)) FROM #opt WHERE ISNULL(BounceReason, '') <> ''
) o
ORDER BY FilterName, FilterValue;
END
GO
