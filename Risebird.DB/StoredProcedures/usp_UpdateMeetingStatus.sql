/****** Object:  StoredProcedure [dbo].[usp_UpdateMeetingStatus]    Script Date: 18-02-2020 5.11.02 PM ******/
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_UpdateMeetingStatus'))
BEGIN
    DROP PROCEDURE dbo.usp_UpdateMeetingStatus
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_UpdateMeetingStatus]
@IRFId nvarchar(100),
@date DateTime

AS
Begin

UPDATE InterviewMeetings
SET confirmationStatus = 2
OUTPUT inserted.interviewer, inserted.candidateEmail, inserted.candidateName, inserted.ClientHrContact, inserted.interviewDate
WHERE IRFId = @IRFId and interviewDate >= @date

End

GO


