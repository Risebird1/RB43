IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_FetchChannelOwnerByInterviewId'))
BEGIN
    DROP PROCEDURE dbo.usp_FetchChannelOwnerByInterviewId
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_FetchChannelOwnerByInterviewId]
    @interviewid INT
AS
BEGIN
    SELECT ClientHrContact AS EmailId
    FROM InterviewMeetings
    WHERE id = @interviewid
END
GO
