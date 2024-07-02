IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_GetSurchargeRequestDetails'))
BEGIN
    DROP PROCEDURE dbo.usp_GetSurchargeRequestDetails
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create PROCEDURE [dbo].[usp_GetSurchargeRequestDetails] 
@actionType int,
@StartDate Datetime,
@EndDate Datetime
As
Begin
If(@actionType=0)
Begin
Select im.id as InterviewId,IRF.id as IRFID,IRF.IRFId as SkillChannel,IRF.JDTiltle,cl.Name as ClientName,im.interviewer,
'Interview' as type,im.Surcharge,im.SurchargeComment as SurchargeComment,NULL AS UserId,im.candidateEmail as candidateEmail,ul.EmailId as ChannelOwner,im.interviewDate
,SuechargeTracker.RequestBy,SuechargeTracker.RequestAt,ic.MeetingStatus
from InterviewMeetings im 
join IRF on im.IRFId = IRF.IRFId
Left Join UserList ul on IRF.RisebirdHr=ul.id
left join [dbo].[InterviewComments] ic on im.id = ic.InterviewId 
Left join Client cl on cl.id=IRF.ClientId
Outer Apply(select top 1 sat.RequestBy,sat.RequestAt from SurchargeActivityTracker sat where sat.RequestType='Raised' and sat.InterviewId= im.id order by sat.RequestAt desc) SuechargeTracker
where im.Surcharge>0 and im.IsSurchargeApprove is null and SuechargeTracker.RequestAt between @StartDate and @EndDate
Union
Select ti.id as InterviewId,IRF.id as IRFID,IRF.IRFId as SkillChannel,IRF.JDTiltle,cl.Name as ClientName,ul.EmailId as interviewer,
'Skill' as type,ti.Surcharge,NULL AS SurchargeComment,ti.UserId as UserId,Null as candidateEmail,ulc.EmailId as ChannelOwner,null as interviewDate,
SuechargeTracker.RequestBy,SuechargeTracker.RequestAt,Null as MeetingStatus
from TransInterviewer ti
  Join UserList ul on ti.UserId=ul.id
  join IRF on ti.IRFId = IRF.id  
  Left Join UserList ulc on IRF.RisebirdHr=ulc.id
  Left join Client cl on cl.id=IRF.ClientId
  Outer Apply(select top 1 sat.RequestBy,sat.RequestAt from SurchargeActivityTracker sat where sat.RequestType='Raised' and sat.IRFId=ti.IRFId and sat.UserId=ti.UserId order by sat.RequestAt desc) SuechargeTracker
where ti.Surcharge>0 and ti.IsSurchargeApprove is null and SuechargeTracker.RequestAt between @StartDate and @EndDate
End
--Approved List
Else
Begin
Select im.id as InterviewId,IRF.id as IRFID,IRF.IRFId as SkillChannel,IRF.JDTiltle,cl.Name as ClientName,im.interviewer,
'Interview' as type,im.Surcharge,im.SurchargeComment as SurchargeComment,NULL AS UserId,im.candidateEmail as candidateEmail,ul.EmailId as ChannelOwner,im.interviewDate
,SuechargeTracker.RequestBy,SuechargeTracker.RequestAt,ic.MeetingStatus
from InterviewMeetings im 
join IRF on im.IRFId = IRF.IRFId
Left Join UserList ul on IRF.RisebirdHr=ul.id
left join [dbo].[InterviewComments] ic on im.id = ic.InterviewId 
Left join Client cl on cl.id=IRF.ClientId
Outer Apply(select top 1 sat.RequestBy,sat.RequestAt from SurchargeActivityTracker sat where sat.RequestType='Approved' and sat.InterviewId= im.id order by sat.RequestAt desc) SuechargeTracker
where  im.IsSurchargeApprove =1 and SuechargeTracker.RequestAt between @StartDate and @EndDate
Union
Select ti.id as InterviewId,IRF.id as IRFID,IRF.IRFId as SkillChannel,IRF.JDTiltle,IRF.ClientName,ul.EmailId as interviewer,
'Skill' as type,ti.Surcharge,NULL AS SurchargeComment,ti.UserId as UserId,Null as candidateEmail,ulc.EmailId as ChannelOwner,null as interviewDate,
SuechargeTracker.RequestBy,SuechargeTracker.RequestAt,Null as MeetingStatus
from TransInterviewer ti
  Join UserList ul on ti.UserId=ul.id
  join IRF on ti.IRFId = IRF.id 
  Left Join UserList ulc on IRF.RisebirdHr=ulc.id
  Left join Client cl on cl.id=IRF.ClientId
  Outer Apply(select top 1 sat.RequestBy,sat.RequestAt from SurchargeActivityTracker sat where sat.RequestType='Approved' and sat.IRFId=ti.IRFId and sat.UserId=ti.UserId order by sat.RequestAt desc) SuechargeTracker
where  ti.IsSurchargeApprove =1 and SuechargeTracker.RequestAt between @StartDate and @EndDate
End
End
GO