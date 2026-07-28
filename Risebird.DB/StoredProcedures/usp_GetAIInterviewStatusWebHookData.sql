/****** Object:  StoredProcedure [dbo].[usp_GetAIInterviewStatusWebHookData]    ******/
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetAIInterviewStatusWebHookData'))
BEGIN
    DROP PROCEDURE dbo.usp_GetAIInterviewStatusWebHookData
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create PROCEDURE [dbo].[usp_GetAIInterviewStatusWebHookData]    
@timeNow datetime  
AS    
Begin    
select aiwh.*, Project.ProjectUniqueid as jobId, cir.Cancellationreason, cir.Cancellationsource from AIInterviewStatusWebHookData aiwh  
join IRF on irf.IRFId = aiwh.IRFId  
outer apply (Select top 1 ProjectUniqueid From ProjectIRFMapping pim join Project pr on pr.id=pim.ProjectId where pim.IRFId=IRF.id) Project   
Outer Apply(select top 1 Cancellationsource,Cancellationreason From InterviewMeetings where candidateEmail=aiwh.CandidateEmailId and IRFId=aiwh.IRFId and confirmationStatus=2 order by interviewdate desc) cir   
End     
GO
