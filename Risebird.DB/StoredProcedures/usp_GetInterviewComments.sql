/****** Object:  StoredProcedure [dbo].[usp_GetInterviewComments]    Script Date: 03-06-2020 7.32.59 PM ******/
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetInterviewComments'))
BEGIN
    DROP PROCEDURE dbo.usp_GetInterviewComments
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

  
CREATE PROCEDURE [dbo].[usp_GetInterviewComments]  
  
@interviewId int  
  
AS  
Begin  
  
SELECT [InterviewStatus], [InterviewerComment], [FraudCheckStatus], [HRComment],[TrainingStatus], [TrainerComment],  
	   [AudioLink], [VideoLink], [L2InterviewStatus], [ClientInterviewStatus] as FinalInterviewMeetingStatus,
	   [MeetingStatus], [OfferStatus], [PreparationStatus], [JoiningStatus], [AadharFileLink],
	   qcfr.QCStatus, qcfr.QCComment

FROM [dbo].[InterviewComments]  
left join QCFeedbackReport qcfr on qcfr.InterviewId =InterviewComments.InterviewId
WHERE  
     InterviewComments.InterviewId = @interviewId  
End  
  
GO


