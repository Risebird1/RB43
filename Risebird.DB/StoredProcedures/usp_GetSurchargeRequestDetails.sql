IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_GetSurchargeRequestDetails'))
BEGIN
    DROP PROCEDURE dbo.usp_GetSurchargeRequestDetails
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create PROCEDURE [dbo].[usp_GetSurchargeRequestDetails] 
@actionType int
As
Begin
If(@actionType=0)
Begin
Select im.id as InterviewId,IRF.id as IRFID,IRF.IRFId as SkillChannel,IRF.JDTiltle,IRF.ClientName,im.interviewer,
'Interview' as type,im.Surcharge,im.SurchargeComment as SurchargeComment,NULL AS UserId
from InterviewMeetings im 
join IRF on im.IRFId = IRF.IRFId
where im.Surcharge>0 and im.IsSurchargeApprove is null
Union
Select ti.id as InterviewId,IRF.id as IRFID,IRF.IRFId as SkillChannel,IRF.JDTiltle,IRF.ClientName,ul.EmailId as interviewer,
'Skill' as type,ti.Surcharge,NULL AS SurchargeComment,ti.UserId as UserId
from TransInterviewer ti
  Join UserList ul on ti.UserId=ul.id
  join IRF on ti.IRFId = IRF.id  
where ti.Surcharge>0 and ti.IsSurchargeApprove is null 
End
--Approved List
Else
Begin
Select im.id as InterviewId,IRF.id as IRFID,IRF.IRFId as SkillChannel,IRF.JDTiltle,IRF.ClientName,im.interviewer,
'Interview' as type,im.Surcharge,im.SurchargeComment as SurchargeComment,NULL AS UserId
from InterviewMeetings im 
join IRF on im.IRFId = IRF.IRFId
where  im.IsSurchargeApprove =1
Union
Select ti.id as InterviewId,IRF.id as IRFID,IRF.IRFId as SkillChannel,IRF.JDTiltle,IRF.ClientName,ul.EmailId as interviewer,
'Skill' as type,ti.Surcharge,NULL AS SurchargeComment,ti.UserId as UserId
from TransInterviewer ti
  Join UserList ul on ti.UserId=ul.id
  join IRF on ti.IRFId = IRF.id  
where  ti.IsSurchargeApprove =1
End
End 
GO