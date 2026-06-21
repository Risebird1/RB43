IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_FetchInterviewDetailsForWhatsApp'))
BEGIN
    DROP PROCEDURE dbo.usp_FetchInterviewDetailsForWhatsApp
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_FetchInterviewDetailsForWhatsApp]
    @interviewid INT
AS
BEGIN
    SELECT id, candidateEmail, interviewDate, CandidateName
    FROM InterviewMeetings
    WHERE id = @interviewid
      AND interviewdate > DATEADD(MINUTE, 330, GETDATE())
      AND confirmationStatus = 1
END
GO
