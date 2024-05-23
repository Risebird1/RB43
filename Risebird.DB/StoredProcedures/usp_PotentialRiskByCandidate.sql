IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_PotentialRiskByCandidate'))
BEGIN
    DROP PROCEDURE dbo.usp_PotentialRiskByCandidate
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create Procedure usp_PotentialRiskByCandidate
@CurrentDate DateTime,
@EmailId varchar(200)
as
Begin
Declare @Role varchar(150)              
Select @Role=pr.Name From UserList ul join Profile pr on pr.id=ul.idprofile where Emailid=@Emailid 
if(@Role='SeniorHR')
BEGIN
select  im.*, ul.EmailId as ChannelOwner,IRF.Id as irfIntId,cfd.Rating,cfd.CreatedOn as RatingAddedOn, itp.RedMarker,
cl.Name as Name, cfd.interviewid,ic.InterviewStatus,ps.IsApproved,cm.id as CandidateMasterId
from CandidateFeedbackDetails cfd
inner join InterviewMeetings im  on cfd.interviewid=im.id
join IRF on im.IRFId=irf.IRFId
join Client cl on irf.ClientId=cl.id 
join UserList ul on ul.id=  irf.RisebirdHr
left join UserList uli on im.interviewer= uli.EmailId  
left join InterviewerProfile itp on itp.UserId = uli.id 
left join [dbo].[InterviewComments] ic on im.id = ic.InterviewId
left join PaymentSLADetails ps on im.id=ps.Interviewid
left join UserList ulc on ulc.EmailId=im.candidateEmail            
join CandidatesMaster cm on cm.UserId=ulc.id 
where cfd.Rating<=3 and Cast(interviewDate as Date)<= @CurrentDate and ul.EmailId=@EmailId
END
ELSE
BEGIN
select  im.*, ul.EmailId as ChannelOwner,IRF.Id as irfIntId,cfd.Rating,cfd.CreatedOn as RatingAddedOn, itp.RedMarker,
cl.Name as Name, cfd.interviewid,ic.InterviewStatus,ps.IsApproved,cm.id as CandidateMasterId
from CandidateFeedbackDetails cfd
inner join InterviewMeetings im  on cfd.interviewid=im.id
join IRF on im.IRFId=irf.IRFId
join Client cl on irf.ClientId=cl.id 
join UserList ul on ul.id=  irf.RisebirdHr
left join UserList uli on im.interviewer= uli.EmailId  
left join InterviewerProfile itp on itp.UserId = uli.id 
left join [dbo].[InterviewComments] ic on im.id = ic.InterviewId
left join PaymentSLADetails ps on im.id=ps.Interviewid
left join UserList ulc on ulc.EmailId=im.candidateEmail            
join CandidatesMaster cm on cm.UserId=ulc.id 
where cfd.Rating<=3 and Cast(interviewDate as Date)<= @CurrentDate --(Cast(interviewDate as Date)> Cast(DATEADD(day,-7,@CurrentDate) as Date) and Cast(interviewDate as Date)<= @CurrentDate)
End
GO