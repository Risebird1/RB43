IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_FetchClientFeedbackByInterviewer'))
BEGIN
    DROP PROCEDURE dbo.usp_FetchClientFeedbackByInterviewer
END
GO

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_FetchClientFeedbackByInterviewer]
    @Emailid VARCHAR(200)
AS
BEGIN
    SET NOCOUNT ON;

    -- Replaces duplicate UNION + UserList join.
    -- Filter meetings by interviewer email directly, then seek comments by InterviewId.
    SELECT
        im.id              AS interviewid,
        im.interviewDate   AS interviewdate,
        im.candidateName   AS Name,
        cfr.Comment,
        cfr.Rating,
        cfr.Rating         AS StarRating
    FROM       dbo.InterviewMeetings im
    INNER JOIN dbo.ClientCommentsForReport cfr ON cfr.InterviewId = im.id
    WHERE im.Interviewer = @Emailid;
END
GO
