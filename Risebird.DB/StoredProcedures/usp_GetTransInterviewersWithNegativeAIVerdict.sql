IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetTransInterviewersWithNegativeAIVerdict'))
BEGIN
    DROP PROCEDURE dbo.usp_GetTransInterviewersWithNegativeAIVerdict
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create PROCEDURE [dbo].[usp_GetTransInterviewersWithNegativeAIVerdict]  --has currently been changed to both positive and negative verdict   
As
Begin

 select distinct ti.id, ResumeLink, ul.EmailId, irf.id as IRFId, 
 irf.JDTiltle as JDTitle, irf.CreatedOn, ti.UserId, ulr.EmailId as ChannelOwner, --ti.OpenAIScreeningResponse, 
 cl.[Name] as CompanyName, ulc.EmailId as ClientHr, irf.irfid as IRFName, ag.AssessmentGroup as SkillChannelAssessmentGroup,
 ipr.CreatedBy as InterviewerProfileAddedBy, ti.IsActive, ti.AIVerdict, irf.JDLink, ti.AIScreeningScore,sti.TotalInterview,Fti.TotalFuture,Rcd.RedMarkerReviewComment,tir.NegativeAIVerdictStatus
 from TransInterviewer ti
 join IRF on irf.id = ti.irfId
 join InterviewerProfile ipr on ipr.id = ti.InterviewerId
 join UserList ul on ul.id = ipr.UserId
 join UserList ulr on ulr.id = irf.RisebirdHr
 join Client cl on cl.id = irf.ClientId
 join UserList ulc on ulc.id = irf.BillingOwner
 join AssessmentGroup ag on ag.id = irf.AssessmentGroupId
 left join TransInterviewerRedMarker tir on tir.IRFId=ti.IRFId and tir.InterviewerId=ipr.Id
 outer Apply (Select Count(id) as TotalFuture from InterviewMeetings where interviewer=ul.EmailId and interviewDate>DATEADD(MINUTE,330,Getdate())) Fti    
 outer Apply (Select Count(id) as TotalInterview from InterviewMeetings where IRFId=IRF.IRFId and interviewDate>DATEADD(MINUTE,330,Getdate())) sti 
 outer Apply (Select Top 1 Comment as RedMarkerReviewComment from RedMarkerStatusDetails where InterviewerUserid=ul.id and IRFId=IRF.id and Type='NegativeAIVerdict' Order by CreatedOn desc) Rcd
 where AIVerdict in ('No') and ti.IsActive = 1-- 'Yes',ti.IsActive = 1 and  --AIVerdict='No' --OpenAIScreeningResponse is null--irfid = @irfId and ti.UserId = @userId
 and (irf.IsPaused = 0 or irf.IsPaused is null) and cast(irf.EndDate as date) >= cast(getdate() as date)
 order by irf.CreatedOn desc

End
GO

 