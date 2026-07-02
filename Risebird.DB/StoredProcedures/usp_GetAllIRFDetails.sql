
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetAllIRFDetails'))
BEGIN
    DROP PROCEDURE dbo.usp_GetAllIRFDetails
END
GO

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_GetAllIRFDetails]
    @startDate DATETIME,
    @endDate   DATETIME,
    @timeNow   DATETIME
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @StartDt DATETIME = CAST(CAST(@startDate AS DATE) AS DATETIME);
    DECLARE @EndDt   DATETIME = DATEADD(DAY, 1, CAST(CAST(@endDate AS DATE) AS DATETIME));

    DECLARE @tbl_Temp TABLE (
        IRFId INT
    );

    INSERT INTO @tbl_Temp
    SELECT DISTINCT IRFId
    FROM   dbo.CandidateStatus
    WHERE  [Status] = 12
      AND  [Date] >= @StartDt
      AND  [Date] <  @EndDt;

    DECLARE @TemporaryCandidateTableVariable TABLE (
        IRFId             INT,
        CandidateMasterId INT,
        UserId            INT
    );

    INSERT INTO @TemporaryCandidateTableVariable
    SELECT
        ca.IRFId,
        ca.CandidateMasterId,
        cm.UserId
    FROM       dbo.Candidate ca
    INNER JOIN dbo.CandidateStatus cs
        ON  ca.IRFId = cs.IRFId
        AND ca.CandidateMasterId = cs.CandidateId
    INNER JOIN dbo.CandidatesMaster cm ON cm.id = ca.CandidateMasterId
    WHERE  cs.[Status] = 12
      AND  cs.[Date] >= @StartDt
      AND  cs.[Date] <  @EndDt
    GROUP BY ca.IRFId, ca.CandidateMasterId, cm.UserId;

    ;WITH CandidateBase AS (
        SELECT
            tctv.IRFId,
            tctv.UserId,
            ul.EmailId      AS CandidateEmail,
            irf.IRFId       AS IrfBusinessId
        FROM       @TemporaryCandidateTableVariable tctv
        INNER JOIN dbo.UserList ul ON ul.id = tctv.UserId
        INNER JOIN dbo.IRF      irf ON irf.id = tctv.IRFId
    ),
    CandidateMeetings AS (
        SELECT
            cb.IRFId,
            cb.UserId,
            im.interviewDate,
            im.confirmationStatus,
            ic.MeetingStatus,
            ic.InterviewStatus,
            rnAll = ROW_NUMBER() OVER (
                PARTITION BY cb.UserId, cb.IRFId
                ORDER BY im.interviewDate DESC
            ),
            rnBeforeNow = ROW_NUMBER() OVER (
                PARTITION BY cb.UserId, cb.IRFId
                ORDER BY CASE WHEN im.interviewDate < @timeNow THEN im.interviewDate END DESC
            )
        FROM       CandidateBase cb
        INNER JOIN dbo.InterviewMeetings im
            ON  im.candidateEmail = cb.CandidateEmail
            AND im.IRFId          = cb.IrfBusinessId
        LEFT JOIN  dbo.InterviewComments ic ON ic.InterviewId = im.id
        WHERE  im.interviewDate >= @StartDt
    ),
    LatestMeeting AS (
        SELECT IRFId, UserId, confirmationStatus, MeetingStatus, InterviewStatus
        FROM   CandidateMeetings
        WHERE  rnAll = 1
    ),
    LatestBeforeNow AS (
        SELECT IRFId, UserId, confirmationStatus, MeetingStatus
        FROM   CandidateMeetings
        WHERE  rnBeforeNow = 1
          AND  interviewDate < @timeNow
    ),
    MeetingAgg AS (
        SELECT
            IRFId,
            UserId,
            IsEverScheduledTillNow = MAX(CASE WHEN interviewDate < @timeNow THEN 1 ELSE 0 END),
            IsFutureScheduled      = MAX(CASE
                WHEN interviewDate >= @timeNow AND confirmationStatus = 1
                THEN 1 ELSE 0 END)
        FROM CandidateMeetings
        GROUP BY IRFId, UserId
    ),
    PerCandidate AS (
        SELECT
            cb.IRFId,
            cb.UserId,
            IsEverScheduledTillNow = ISNULL(ma.IsEverScheduledTillNow, 0),
            IsFutureScheduled      = ISNULL(ma.IsFutureScheduled, 0),
            IsNeverScheduled       = CASE WHEN ma.UserId IS NULL THEN 1 ELSE 0 END,
            IsCompleted = CASE
                WHEN lm.confirmationStatus = 1
                 AND lm.MeetingStatus IN (4, 6, 7, 8)
                THEN 1 ELSE 0 END,
            IsSelected = CASE
                WHEN lm.confirmationStatus = 1
                 AND lm.InterviewStatus IN (2, 4)
                THEN 1 ELSE 0 END,
            IsRejected = CASE
                WHEN lm.confirmationStatus = 1
                 AND lm.InterviewStatus IN (1, 3)
                THEN 1 ELSE 0 END,
            IsEligibleForReschedule = CASE
                WHEN lm.confirmationStatus = 2
                  OR lm.MeetingStatus IN (1, 3, 5)
                THEN 1 ELSE 0 END,
            IsReportPending = CASE
                WHEN lbn.confirmationStatus = 1
                 AND (lbn.MeetingStatus = 2 OR lbn.MeetingStatus IS NULL)
                THEN 1 ELSE 0 END
        FROM       CandidateBase cb
        LEFT JOIN  MeetingAgg      ma  ON ma.UserId = cb.UserId AND ma.IRFId = cb.IRFId
        LEFT JOIN  LatestMeeting   lm  ON lm.UserId = cb.UserId AND lm.IRFId = cb.IRFId
        LEFT JOIN  LatestBeforeNow lbn ON lbn.UserId = cb.UserId AND lbn.IRFId = cb.IRFId
    ),
    IrfMetrics AS (
        SELECT
            IRFId,
            CandidateCount            = COUNT(*),
            ScheduledMeetings         = SUM(IsEverScheduledTillNow),
            FutureScheduledMeetings   = SUM(IsFutureScheduled),
            CompletedInterviews       = SUM(IsCompleted),
            SelectedMeetings          = SUM(IsSelected),
            RejectedMeetings          = SUM(IsRejected),
            NotScheduledCount         = SUM(IsNeverScheduled),
            ReScheduledCount          = SUM(IsEligibleForReschedule),
            PendingCount              = SUM(IsReportPending)
        FROM PerCandidate
        GROUP BY IRFId
    )
    SELECT
        irf.id,
        irf.IRFId,
        irf.JDTiltle,
        cl.[Name] AS ClientCompName,
        irf.JobCountry,
        ISNULL(ulb.EmailId, '') AS BillingOwnerEmailId,
        ul.EmailId AS RisebirdHrEmail,
        ISNULL(m.CandidateCount, 0) AS CandidateCount,
        ISNULL(m.ScheduledMeetings, 0) AS ScheduledMeetings,
        ISNULL(m.FutureScheduledMeetings, 0) AS FutureScheduledMeetings,
        ISNULL(m.CompletedInterviews, 0) AS CompletedInterviews,
        ISNULL(m.SelectedMeetings, 0) AS SelectedMeetings,
        ISNULL(m.RejectedMeetings, 0) AS RejectedMeetings,
        ISNULL(m.NotScheduledCount, 0) AS NotScheduledCount,
        ISNULL(m.ReScheduledCount, 0) AS ReScheduledCount,
        ISNULL(m.PendingCount, 0) AS PendingCount
    FROM       dbo.IRF irf
    INNER JOIN @tbl_Temp tmp ON tmp.IRFId = irf.id
    INNER JOIN dbo.Client cl ON cl.id = irf.ClientId
    INNER JOIN dbo.UserList ul ON ul.id = irf.RisebirdHr
    LEFT JOIN  dbo.UserList ulb ON ulb.id = irf.BillingOwner
    LEFT JOIN  IrfMetrics m ON m.IRFId = irf.id;
END
GO
