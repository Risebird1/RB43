CREATE OR ALTER PROCEDURE [dbo].[usp_GetInterviewerStatsAndRatingsDetails]
    @timeNow DATETIME,
    @irfId INT = NULL,
    @type VARCHAR(30),
    @interviewerUserId INT,
    @clientEmailId VARCHAR(100) = NULL,
    @FromIst DATE = NULL,
    @ToIst DATE = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @yesterdayDate DATE = CAST(DATEADD(DAY, -1, @timeNow) AS DATE);
    DECLARE @IRFName VARCHAR(30);
    IF (@irfId IS NOT NULL)
    BEGIN
        SET @IRFName = (SELECT IRFId FROM IRF WHERE id = @irfId);
    END

    DECLARE @interviewerEmail VARCHAR(100) = (SELECT EmailId FROM UserList WHERE id = @interviewerUserId);

    -- Optional IST window (same pattern as IRF audit directory). When both NULL, keep legacy behavior.
    IF @FromIst IS NOT NULL OR @ToIst IS NOT NULL
    BEGIN
        IF @ToIst IS NULL
            SET @ToIst = CAST(DATEADD(MINUTE, 330, GETUTCDATE()) AS DATE);
        IF @FromIst IS NULL
            SET @FromIst = DATEADD(DAY, -30, @ToIst);
        IF @FromIst > @ToIst
            SET @FromIst = DATEADD(DAY, -30, @ToIst);
        IF DATEDIFF(DAY, @FromIst, @ToIst) > 90
            SET @FromIst = DATEADD(DAY, -90, @ToIst);
    END

    DECLARE @tbl_RelevantInterviews TABLE (
        id INT,
        interviewDate DATETIME,
        candidateEmail VARCHAR(200),
        SkillChannel VARCHAR(200)
    );

    INSERT INTO @tbl_RelevantInterviews
    SELECT im.id, interviewDate, candidateEmail, IRFId
    FROM InterviewMeetings im
    WHERE ((CAST(im.interviewDate AS DATE) >= @yesterdayDate AND @irfId IS NULL) OR @irfId IS NOT NULL)
      AND confirmationStatus = 1
      AND (@IRFName IS NULL OR im.IRFId = @IRFName)
      AND interviewer = @interviewerEmail
      AND (@clientEmailId IS NULL OR (im.ClientHrContact = @clientEmailId OR im.staffingAgency = @clientEmailId))
      AND (
            @FromIst IS NULL
         OR (
                CAST(DATEADD(MINUTE, 330, im.interviewDate) AS DATE) >= @FromIst
            AND CAST(DATEADD(MINUTE, 330, im.interviewDate) AS DATE) <= @ToIst
            )
          );

    DECLARE @tbl_InterviewerJoinTime TABLE (
        interviewId INT,
        interviewer VARCHAR(200),
        JoiningTime DATETIME
    );

    INSERT INTO @tbl_InterviewerJoinTime
    SELECT interviewid, ijt.interviewer, MAX(Joinedon)
    FROM InterviewMeetingsJoinTime ijt
    JOIN InterviewMeetings im ON im.id = ijt.interviewid
    WHERE ijt.interviewer = @interviewerEmail
      AND (@clientEmailId IS NULL OR (im.ClientHrContact = @clientEmailId OR im.staffingAgency = @clientEmailId))
    GROUP BY interviewid, ijt.interviewer;

    DECLARE @tbl_ResultInterviews TABLE (
        interviewId INT,
        interviewDate DATETIME,
        candidateEmail VARCHAR(200),
        SkillChannel VARCHAR(200),
        Client VARCHAR(50),
        MeetingStatus INT,
        InterviewStatus INT,
        ResultType VARCHAR(30),
        InterviewerJoinTime DATETIME,
        irfId INT,
        isTechQC BIT,
        JDTiltle VARCHAR(200),
        ProjectUniqueid VARCHAR(200),
        TechQCRequired BIT
    );

    INSERT INTO @tbl_ResultInterviews
    SELECT
        im.id,
        im.interviewDate,
        im.candidateEmail,
        im.SkillChannel,
        cl.[Name],
        ic.MeetingStatus,
        ic.InterviewStatus,
        @type,
        tijt.JoiningTime,
        IRF.id AS irfId,
        ISNULL(tqd.IsActive, 0) AS isTechQC,
        CASE
            WHEN irf.SkillSetsevaluate > 1 AND ISNULL(irf.JDTitleTwo, '') != ''
            THEN irf.JDTiltle + ' and ' + irf.JDTitleTwo
            ELSE irf.JDTiltle
        END AS JDTiltle,
        pr.ProjectUniqueid,
        ISNULL(IRF.IsTechQC, 0) AS TechQCRequired
    FROM @tbl_RelevantInterviews im
    LEFT JOIN InterviewComments ic ON ic.InterviewId = im.id
    JOIN IRF ON irf.IRFId = im.SkillChannel
    JOIN Client cl ON cl.id = irf.ClientId
    LEFT JOIN ProjectIRFMapping pim ON pim.IRFId = IRF.id
    LEFT JOIN Project pr ON pr.id = pim.ProjectId
    LEFT JOIN @tbl_InterviewerJoinTime tijt ON tijt.interviewId = im.id
    LEFT JOIN TechQualityDetails tqd ON tqd.Interviewid = im.id
    WHERE (@type = 'all')
       OR (@type = 'completed' AND ic.MeetingStatus IN (4, 6, 7, 8))
       OR (@type = 'selected' AND (ic.InterviewStatus = 2) AND (ic.MeetingStatus = 4 OR ic.MeetingStatus = 6))
       OR (@type = 'borderline' AND (ic.InterviewStatus = 4) AND (ic.MeetingStatus = 4 OR ic.MeetingStatus = 8))
       OR (@type = 'rejected' AND (ic.InterviewStatus = 3) AND (ic.MeetingStatus = 4 OR ic.MeetingStatus = 7))
       OR (@type = 'proxy' AND ic.InterviewStatus = 1 AND ic.MeetingStatus = 4)
       OR (@type = 'no show interviewer' AND (ic.MeetingStatus = 5))
       OR (@type = 'no show candidate' AND (ic.MeetingStatus = 1))
       OR (@type = 'no show' AND (ic.MeetingStatus = 1 OR ic.MeetingStatus = 5))
       OR (@type = 'dropped' AND ic.MeetingStatus = 3)
       OR (@type = 'pending' AND ((im.interviewDate < @timeNow) AND ((ic.MeetingStatus = 4 AND ic.InterviewStatus IS NULL) OR (ic.MeetingStatus IS NULL) OR ic.MeetingStatus = 2)))
       OR (@type = 'scheduled' AND ic.MeetingStatus = 2 AND im.interviewDate > @timeNow);

    SELECT * FROM @tbl_ResultInterviews;
END
GO
