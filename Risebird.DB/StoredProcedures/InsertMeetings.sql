IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.InsertMeetings'))
BEGIN
    DROP PROCEDURE dbo.InsertMeetings
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[InsertMeetings]
@IRFId varchar(200),
@interviewDate varchar(200),
@slot varchar(100),
@interviewDuration int,
@candidateEmail varchar(200),
@hrRiseBirdAssinged varchar(200),
@resumeLink varchar(MAX),
@zoomLink varchar(MAX),
@ClientHrContact varchar(200),
@comfirmationStatus varchar(100),
@prefferedLocation varchar(200),
@expectedPerHourRate varchar(200),
@joiningTime varchar(200),
@staffingAgenecy varchar(200),
@interviewOnDevice varchar(500)
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
		   ,[IntervIewOnDevice]
           ,[sCreatedBy]
           ,[dtCreatedOn]
           )
     VALUES
           (@IRFId,
@interviewDate,
@slot,
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
@interviewOnDevice,
'Clendly',
GETUTCDATE()  
)

End

ELSE

 Begin
 UPDATE [dbo].[InterviewMeetings]
   SET [interviewDate] = @interviewDate
      ,[comfirmationStatus] = @comfirmationStatus
 WHERE     candidateEmail = @candidateEmail and interviewDate=@interviewDate
 
 
End
END
GO



