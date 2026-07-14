IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_DriveWeekendInterviews'))
BEGIN
    DROP PROCEDURE dbo.usp_DriveWeekendInterviews
END
GO

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_DriveWeekendInterviews]
    @startDate date,
    @endDate date
AS
BEGIN
    SELECT
        im.IRFId AS IRFID,
        im.interviewDate,
        CASE
            WHEN LTRIM(RTRIM(CAST(im.interviewSlot AS varchar(100)))) IN ('1', 'Morning') THEN 1
            WHEN LTRIM(RTRIM(CAST(im.interviewSlot AS varchar(100)))) IN ('2', 'Evening') THEN 2
            ELSE TRY_CAST(LTRIM(RTRIM(CAST(im.interviewSlot AS varchar(100)))) AS INT)
        END AS interviewSlot,
        im.interviewer,
        ic.interviewStatus,
        ic.InterviewerComment,
        ic.VideoLink,
        QCF.InterviewId AS QCInterview
    FROM InterviewMeetings im
    LEFT JOIN InterviewComments ic ON im.id = ic.InterviewId
    LEFT JOIN QCFeedbackReport QCF ON im.id = QCF.InterviewId
    WHERE CAST(im.interviewDate AS date) >= @startDate
      AND CAST(im.interviewDate AS date) <= @endDate
      AND im.confirmationStatus = 1
END
GO
