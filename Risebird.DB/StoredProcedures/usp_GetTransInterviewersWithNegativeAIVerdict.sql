IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetTransInterviewersWithNegativeAIVerdict'))
BEGIN
    DROP PROCEDURE dbo.usp_GetTransInterviewersWithNegativeAIVerdict
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create PROCEDURE [dbo].[usp_GetTransInterviewersWithNegativeAIVerdict]     
As
Begin

 select distinct top 100 ti.id, ResumeLink, ul.EmailId, irf.id as IRFId, 
 irf.JDTiltle as JDTitle, irf.CreatedOn, ti.UserId, ulr.EmailId as ChannelOwner, --ti.OpenAIScreeningResponse, 
 cl.[Name] as CompanyName, ulc.EmailId as ClientHr, irf.irfid as IRFName
 from TransInterviewer ti
 join IRF on irf.id = ti.irfId
 join InterviewerProfile ipr on ipr.id = ti.InterviewerId
 join UserList ul on ul.id = ipr.UserId
 join UserList ulr on ulr.id = irf.RisebirdHr
 join Client cl on cl.id = irf.ClientId
 join UserList ulc on ulc.id = irf.BillingOwner
 where ti.IsActive = 1 and  AIVerdict='No' --OpenAIScreeningResponse is null--irfid = @irfId and ti.UserId = @userId
 order by irf.CreatedOn desc

End
GO