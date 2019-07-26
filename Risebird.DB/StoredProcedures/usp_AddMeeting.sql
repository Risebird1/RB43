-- =============================================
-- SP Name:  dbo.usp_AddMeeting
-- Author: Prem     
-- Create date: 7th July 2019
-- Description: Add Meeting
-- =============================================
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_AddMeeting'))
BEGIN
    DROP PROCEDURE dbo.usp_AddMeeting
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_AddMeeting]
@IRFId varchar(200),
@interviewDate varchar(200),
@interviewSlot varchar(100),
@interviewDuration int,
@candidateEmail varchar(200),
@hrRiseBirdAssinged varchar(200),
@resumeLink varchar(MAX),
@zoomLink varchar(MAX),
@ClientHrContact varchar(200),
@comfirmationStatus varchar(100),
@interviewer varchar(200),
@prefferedLocation varchar(200),
@expectedPerHourRate varchar(200),
@joiningTime varchar(200),
@staffingAgenecy varchar(200)
AS
Begin

IF NOT EXISTS (SELECT 1   FROM   InterviewMeetings    WHERE  candidateEmail = @candidateEmail and interviewDate=@interviewDate)
Begin
INSERT INTO [dbo].[InterviewMeetings]
           ([IRFId]
           ,[interviewDate]
           ,[interviewSlot]
           ,[interviewDuration]
           ,[candidateEmail]
           ,[ClientHrContact]
           ,[resumeLink]
           ,[comfirmationStatus]
           ,[zoomLink]
           ,[risebirdHR]
		   ,[PreferredLocation]
		   ,[ExpectedPerHourRate]
		   ,[joiningTime]
		   ,[staffingAgenecy]		   
           ,[sCreatedBy]
           ,[dtCreatedOn]
           )
     VALUES
           (@IRFId,
@interviewDate,
@interviewSlot,
@interviewDuration,
@candidateEmail,
@ClientHrContact,
@resumeLink,
@comfirmationStatus,
@zoomLink,
@hrRiseBirdAssinged,
@prefferedLocation,
@expectedPerHourRate,
@joiningTime,
@staffingAgenecy,
'System',
GETUTCDATE()  
)

End
END


GO



