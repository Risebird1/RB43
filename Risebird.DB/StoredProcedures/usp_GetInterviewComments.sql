/****** Object:  StoredProcedure [dbo].[usp_GetInterviewComments]    Script Date: 03-06-2020 7.32.59 PM ******/
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetInterviewComments'))
BEGIN
    DROP PROCEDURE dbo.usp_GetInterviewComments
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

  
Create PROCEDURE [dbo].[usp_GetInterviewComments]  
  
@interviewId int  
  
AS  
Begin  
  
SELECT [InterviewStatus], [InterviewerComment], [FraudCheckStatus], ic.[HRComment],[TrainingStatus], [TrainerComment],  
	   [AudioLink], [VideoLink], [L2InterviewStatus], [ClientInterviewStatus] as FinalInterviewMeetingStatus,
	   [MeetingStatus], [OfferStatus], [PreparationStatus], [JoiningStatus], [AadharFileLink],[CandidateAvailable],
	   ul.EmailId as candidateEmail, irf.id as irfIntId, irf.IRFId as IRFName, im.interviewDate,
	   (CASE WHEN CHARINDEX('|', JDTiltle) > 0 THEN SUBSTRING(JDTiltle, 1, CHARINDEX('|', JDTiltle)-1) ELSE JDTiltle END) as PrimarySkill, im.interviewDate

FROM [dbo].[InterviewComments] ic 
join InterviewMeetings im on im.id = ic.InterviewId
join IRF on irf.IRFId=im.IRFId
join UserList ul on ul.EmailId = im.candidateEmail
WHERE  
     ic.InterviewId =@interviewId  
End  
   
  
GO


