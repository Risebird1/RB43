      IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_ListOfCancelledInterviewInterviwer'))
BEGIN
    DROP PROCEDURE dbo.usp_ListOfCancelledInterviewInterviwer
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_ListOfCancelledInterviewInterviwer]      
@candidateEmail varchar(200),    
@interviewDate  varchar(200),
@IRFID varchar(200)
AS      
Begin   

IF EXISTS (Select 1 from IRF where IRFId =@IRFId)
	BEGIN
		select interviewer from [dbo].[InterviewMeetings]      
		where candidateEmail = @candidateEmail and interviewDate= @interviewDate and confirmationStatus =2 
	END
ELSE
	BEGIN
		select interviewer from [dbo].[UnapprovedInterviewerMeetings]      
		where candidateEmail = @candidateEmail and interviewDate= @interviewDate and confirmationStatus =2 
	END
      
End 