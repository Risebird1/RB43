IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.InsertMeetings'))
BEGIN
    DROP PROCEDURE dbo.InsertMeetings
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[InsertMeetings]
@clientName varchar(200),
@JobRole varchar(200),
@interviewDate varchar(200),
@interviewDuration varchar(50),
@slot varchar(100),
@primaryskill varchar(500),
@candidateEmail varchar(200),
@hrRiseBirdAssinged varchar(200),
@resumeLink varchar(MAX),
@zoomLink varchar(MAX),
@eventGeneratedOn varchar(100),
@eventGeneratedBy varchar(100),
@ClientHrContact varchar(200),
@comfirmationStatus varchar(100)

AS
Begin

IF NOT EXISTS (SELECT 1   FROM   InterviewMeetings    WHERE  candidateEmail = @candidateEmail and clientName=@clientName and primaryskill=@primaryskill)
Begin
INSERT INTO [dbo].[InterviewMeetings]
           ([clientName]
           ,[JobRole]
           ,[interviewDate]
           ,[interviewSlot]
           ,[interviewDuration]
           ,[primaryskill]
           ,[candidateEmail]
           ,[ClientHrContact]
           ,[resumeLink]
           ,[comfirmationStatus]
           ,[zoomLink]
           ,[risebirdHR]
           ,[sCreatedBy]
           ,[dtCreatedOn]
           )
     VALUES
           (@clientName,
@JobRole,
@interviewDate,
@slot,
@interviewDuration,
@primaryskill,
@candidateEmail,
@ClientHrContact,
@resumeLink,
@comfirmationStatus,
@zoomLink,
@hrRiseBirdAssinged,
@eventGeneratedBy,
@eventGeneratedOn
)

End

ELSE

 Begin
 UPDATE [dbo].[InterviewMeetings]
   SET [interviewDate] = @interviewDate
      ,[comfirmationStatus] = @comfirmationStatus
 WHERE     candidateEmail = @candidateEmail and clientName=@clientName and primaryskill=@primaryskill
 
 
End
END
GO



