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
		select im.interviewer from [dbo].[InterviewMeetings] im
		join [dbo].[UserList] ul on ul.EmailId = im.interviewer and ul.IsActive = 1
		where im.candidateEmail = @candidateEmail and im.interviewDate= @interviewDate and im.confirmationStatus =2 
	END
ELSE
	BEGIN
		select uim.interviewer from [dbo].[UnapprovedInterviewerMeetings] uim
		join [dbo].[UserList] ul on ul.EmailId = uim.interviewer and ul.IsActive = 1
		where uim.candidateEmail = @candidateEmail and uim.interviewDate= @interviewDate and uim.confirmationStatus =2 
	END
      
End 