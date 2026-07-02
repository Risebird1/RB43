/****** Object:  StoredProcedure [dbo].[usp_GetDateWiseMeetingsSlots]    Script Date: 24-07-2020 9.15.24 PM ******/
-- Performance (2026-06-17): sargable date range + single IRF aggregation CTE (replaces 13 correlated subqueries per row)

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetDateWiseMeetingsSlots'))
BEGIN
    DROP PROCEDURE dbo.usp_GetDateWiseMeetingsSlots
END
GO

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_GetDateWiseMeetingsSlots]
    @dateValue DATETIME,
    @Emailid   VARCHAR(150)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @DayStart DATETIME = CAST(CAST(@dateValue AS DATE) AS DATETIME);
    DECLARE @DayEnd   DATETIME = DATEADD(DAY, 1, @DayStart);
    DECLARE @NowIst   DATETIME = DATEADD(MINUTE, 330, GETUTCDATE());
    DECLARE @Role     VARCHAR(150);

    SELECT @Role = pr.Name
    FROM   dbo.UserList ul
    JOIN   dbo.Profile pr ON pr.id = ul.idProfile
    WHERE  ul.EmailId = @Emailid;

    ;WITH DayMeetings AS (
        SELECT
            im.IRFId,
            im.id,
            im.interviewSlot,
            im.interviewer,
            im.interviewDate,
            ic.InterviewStatus,
            ic.MeetingStatus
        FROM       dbo.InterviewMeetings im
        LEFT JOIN  dbo.InterviewComments ic ON ic.InterviewId = im.id
        WHERE  im.confirmationStatus = 1
          AND  im.interviewDate >= @DayStart
          AND  im.interviewDate <  @DayEnd
    ),
    SlotAgg AS (
        SELECT
            dm.IRFId,
            Morning           = SUM(CASE WHEN dm.interviewSlot = 1 THEN 1 ELSE 0 END),
            Evening           = SUM(CASE WHEN dm.interviewSlot = 2 THEN 1 ELSE 0 END),
            MorningUnassinged = SUM(CASE WHEN dm.interviewSlot = 1 AND dm.interviewer IS NULL THEN 1 ELSE 0 END),
            EveningUnassinged = SUM(CASE WHEN dm.interviewSlot = 2 AND dm.interviewer IS NULL THEN 1 ELSE 0 END),
            total             = COUNT(*),
            Proxy             = SUM(CASE WHEN dm.InterviewStatus = 1 THEN 1 ELSE 0 END),
            Selected          = SUM(CASE WHEN dm.InterviewStatus = 2 THEN 1 ELSE 0 END),
            Rejected          = SUM(CASE WHEN dm.InterviewStatus IN (3, 5) THEN 1 ELSE 0 END),
            BorderLine        = SUM(CASE WHEN dm.InterviewStatus = 4 THEN 1 ELSE 0 END),
            NoShowCount       = SUM(CASE WHEN dm.InterviewStatus IS NULL AND dm.MeetingStatus = 1 THEN 1 ELSE 0 END),
            ScheduledCount    = COUNT(*),
            DroppedCount      = SUM(CASE WHEN dm.InterviewStatus IS NULL AND dm.MeetingStatus = 3 THEN 1 ELSE 0 END),
            Pending           = SUM(CASE WHEN dm.interviewDate < @NowIst AND dm.MeetingStatus = 2 THEN 1 ELSE 0 END)
        FROM DayMeetings dm
        GROUP BY dm.IRFId
    ),
    QCPendingCounts AS (
        SELECT
            inm.IRFId,
            QCPending = COUNT(inm.id)
        FROM       dbo.InterviewMeetings inm
        INNER JOIN dbo.InterviewComments inc ON inc.InterviewId = inm.id
        LEFT JOIN  dbo.QCFeedbackReport  iqc ON inm.id = iqc.Interviewid
        WHERE  inm.confirmationStatus = 1
          AND  inm.interviewDate >= @DayStart
          AND  inm.interviewDate <  @DayEnd
          AND  inc.InterviewStatus IN (2, 4)
          AND  iqc.id IS NULL
        GROUP BY inm.IRFId
    )
    SELECT DISTINCT
        irf.IRFId,
        (irf.IRFId + '(' + CASE
            WHEN CHARINDEX('|', irf.JDTiltle) != 0
            THEN SUBSTRING(irf.JDTiltle, 0, CHARINDEX('|', irf.JDTiltle))
            ELSE irf.JDTiltle
        END + ')') AS IRFnSkill,
        sa.Morning,
        sa.Evening,
        sa.EveningUnassinged,
        sa.MorningUnassinged,
        sa.total,
        sa.Proxy,
        sa.Selected,
        sa.Rejected,
        sa.BorderLine,
        sa.NoShowCount,
        sa.ScheduledCount,
        sa.DroppedCount,
        sa.Pending,
        ISNULL(qc.QCPending, 0) AS QCPending
    FROM       SlotAgg sa
    INNER JOIN dbo.IRF irf ON irf.IRFId = sa.IRFId
    LEFT JOIN  QCPendingCounts qc ON qc.IRFId = sa.IRFId
    LEFT JOIN  dbo.SeniorHr sh ON sh.ClientId = irf.ClientId
    LEFT JOIN  dbo.UserList ulsh ON ulsh.id = sh.UserId
    WHERE  (
               @Role NOT IN ('SeniorHR', 'SupportAdmin')
            OR ulsh.EmailId = @Emailid
           )
    ORDER BY sa.total DESC;
END
GO
