IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_SaveInterviewerConvertedFromDocResumeLink'))
BEGIN
    DROP PROCEDURE dbo.usp_SaveInterviewerConvertedFromDocResumeLink
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create PROCEDURE [dbo].[usp_SaveInterviewerConvertedFromDocResumeLink]       
@transInterviewerId int,  
@convertedResumeLink varchar(2000)  
As  
Begin  
  
if(@convertedResumeLink is not null )
Begin
	
	update InterviewerProfile set ConvertedFromDocResumeLink =@convertedResumeLink where UserId = (select top 1 UserId from TransInterviewer where id = @transInterviewerId)

End  
  
End  