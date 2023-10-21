IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_PotentialRiskByCandidate'))
BEGIN
    DROP PROCEDURE dbo.usp_PotentialRiskByCandidate
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create Procedure usp_PotentialRiskByCandidate
@CurrentDate DateTime
as
Begin
select  im.*, ul.EmailId as ChannelOwner,IRF.Id as irfIntId,cfd.Rating,cfd.CreatedOn as RatingAddedOn, itp.RedMarker,
cl.Name as Name, cfd.interviewid,IRF.id as irfIntId
from CandidateFeedbackDetails cfd
inner join InterviewMeetings im  on cfd.interviewid=im.id
join IRF on im.IRFId=irf.IRFId
join Client cl on irf.ClientId=cl.id 
join UserList ul on ul.id=  irf.RisebirdHr
left join UserList uli on im.interviewer= uli.EmailId  
left join InterviewerProfile itp on itp.UserId = uli.id        
where cfd.Rating<=3 and (Cast(interviewDate as Date)> Cast(DATEADD(day,-7,@CurrentDate) as Date) and Cast(interviewDate as Date)<= @CurrentDate)
End
GO