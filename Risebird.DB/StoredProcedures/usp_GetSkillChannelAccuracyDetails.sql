IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_GetSkillChannelAccuracyDetails'))
BEGIN
    DROP PROCEDURE dbo.usp_GetSkillChannelAccuracyDetails
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
CREATE Procedure usp_GetSkillChannelAccuracyDetails  
@irfId int,  
@type varchar(20)  
AS  
BEGIN  
--High Selection Rates  
if(@type='Selection')  
begin  
declare @HighSelect as Table  
(  
interviewer varchar(150),  
selectionrate int  
)  
;With CTE as(  
select ir.id,ir.IRFId,im.interviewer, count(distinct ic.id) TotalCompletedMeetings,count(distinct ics.id) selectedMeetings,ul.id as UserId from IRF ir  
join InterviewMeetings im on im.IRFId=ir.IRFId  
left join UserList ul on ul.EmailId=im.interviewer  
left join InterviewComments ic on ic.InterviewId = im.id  and (ic.MeetingStatus in (4, 6, 7, 8))  
left join InterviewComments ics on ics.InterviewId = im.id and (ics.InterviewStatus = 2 or ics.InterviewStatus = 4)  
where  ir.id= @irfId   
group by ir.id,ir.IRFId,ul.id,im.interviewer)  
Select id,IRFId,UserId,interviewer,TotalCompletedMeetings,selectedMeetings as TotalSelected, case when TotalCompletedMeetings>0 then(ISNULL(selectedMeetings,0)*100/TotalCompletedMeetings) else 0 end selectionrate From CTE   
where case when TotalCompletedMeetings>0 then(ISNULL(selectedMeetings,0)*100/TotalCompletedMeetings) else 0 end>=40  
end  
--High Rejection Rate  
else if(@type='Rejection')  
begin  
declare @HighReject as Table  
(  
interviewer varchar(150),  
rejectionrate int  
)  
;With CTE as(  
select ir.id,ir.IRFId,im.interviewer, count(distinct ic.id) TotalCompletedMeetings,count(distinct ics.id) rejectmeetings,ul.id as UserId from IRF ir  
join InterviewMeetings im on im.IRFId=ir.IRFId  
left join UserList ul on ul.EmailId=im.interviewer  
left join InterviewComments ic on ic.InterviewId = im.id  and (ic.MeetingStatus in (4, 6, 7, 8))  
left join InterviewComments ics on ics.InterviewId = im.id and (ics.InterviewStatus = 3 or ics.InterviewStatus = 5)   
where  ir.id= @irfId   
group by ir.id,ir.IRFId,ul.id,im.interviewer)  
Select id,IRFId,UserId,interviewer,TotalCompletedMeetings,rejectmeetings as TotalRejected ,case when TotalCompletedMeetings>0 then(ISNULL(rejectmeetings,0)*100/TotalCompletedMeetings) else 0 end rejectionrate From CTE   
where case when TotalCompletedMeetings>0 then(ISNULL(rejectmeetings,0)*100/TotalCompletedMeetings) else 0 end>=80  
end  
--Potential risk by candidate   
else if(@type='NegativeCandidate')  
begin  
select im.interviewer,count(distinct im.id) NegativeRating,ic.TotalCompletedMeetings from IRF ir  
join InterviewMeetings im on im.IRFId=ir.IRFId  
join CandidateFeedbackDetails cd on cd.InterviewId=im.id  
outer Apply (Select Count(distinct ima.id) as TotalCompletedMeetings from InterviewMeetings ima join InterviewComments ica on ica.InterviewId=ima.id where ica.MeetingStatus in (4, 6, 7, 8) and ima.IRFId=ir.IRFId and im.interviewer=ima.interviewer) ic
where cd.Rating<4 and ir.id=@irfId  
group by im.interviewer,ic.TotalCompletedMeetings  
end  
--Potential risk by client   
else if(@type='PotentialRiskByClient')  
begin  
select im.interviewer,count(distinct im.id) NegativeRating,ic.TotalCompletedMeetings      
from  ClientCommentsForReport ccfr       
join InterviewMeetings im on ccfr.InterviewId=im.id      
join IRF on im.IRFId=irf.IRFId      
outer Apply (Select Count(distinct ima.id) as TotalCompletedMeetings from InterviewMeetings ima join InterviewComments ica on ica.InterviewId=ima.id where ica.MeetingStatus in (4, 6, 7, 8) and ima.IRFId=IRF.IRFId and im.interviewer=ima.interviewer) ic
where ccfr.Rating<=3 and IRF.id=@irfId   
group by im.interviewer,ic.TotalCompletedMeetings 
end  
--Redo Request for interviews   
else if(@type='RedoRequest')  
begin  
Select im.interviewDate,im.interviewer,case when ulc.LastName is null then ulc.FirstName                  
else RTRIM(LTRIM(ulc.FirstName)) + SPACE(1) + RTRIM(LTRIM(ulc.LastName))                  
end as candidateName ,  
ulc.EmailId as candidateEmail,IRF.id as irfIntId, ri.CreatedBy, ri.Createdon,ulco.EmailId as ChannelOwner  
from RedoInterviews ri join InterviewMeetings im on ri.interviewid=im.id  
inner join IRF on im.IRFId=irf.IRFId  
left join UserList ulc on im.candidateEmail= ulc.EmailId   
left join UserList ulco on ulco.id=irf.RisebirdHr
where ISNULL(Approved,0)=1 and IRF.id=@irfId
end  
--Select interview short comment  
else if(@type='LessSelectComment')  
begin  
Select im.interviewer,Count(distinct im.id) as TotallessComment,ica.TotalCompletedMeetings 
from IRF ir join Interviewmeetings im on im.IRFId=ir.IRFId  
join InterviewComments ic on im.id=ic.InterviewId   
outer Apply (Select Count(distinct ima.id) as TotalCompletedMeetings from InterviewMeetings ima join InterviewComments ica on ica.InterviewId=ima.id where ica.MeetingStatus in (4, 6, 7, 8) and ima.IRFId=im.IRFId and im.interviewer=ima.interviewer) ica
where confirmationStatus=1 and ir.id=@irfId  
and (ic.MeetingStatus=4 or ic.MeetingStatus=6 or ic.MeetingStatus=7 or ic.MeetingStatus=8)   
and (ic.InterviewStatus=2 or ic.InterviewStatus=4)  
and Len(ic.InterviewerComment)<500   
group by im.interviewer,ica.TotalCompletedMeetings
end  
--Reject interview short comment  
else if(@type='LessRejectComment')  
begin  
Select im.interviewer,Count(distinct im.id) as TotallessComment,ica.TotalCompletedMeetings 
from IRF ir join Interviewmeetings im on im.IRFId=ir.IRFId  
join InterviewComments ic on im.id=ic.InterviewId   
outer Apply (Select Count(distinct ima.id) as TotalCompletedMeetings from InterviewMeetings ima join InterviewComments ica on ica.InterviewId=ima.id where ica.MeetingStatus in (4, 6, 7, 8) and ima.IRFId=im.IRFId and im.interviewer=ima.interviewer) ica
where confirmationStatus=1 and ir.id=@irfId  
and (ic.MeetingStatus=4 or ic.MeetingStatus=6 or ic.MeetingStatus=7 or ic.MeetingStatus=8)   
and (ic.InterviewStatus=3 or ic.InterviewStatus=5)  
and Len(ic.InterviewerComment)<250   
group by im.interviewer,ica.TotalCompletedMeetings
end  
--interview less question added
else if(@type='LessQuestions')  
begin  
Select im.interviewer,Count(distinct im.id) as TotallessQuestions,ica.TotalCompletedMeetings
from IRF ir join InterviewMeetings im on im.IRFId=ir.IRFId join QuestionsDiscussed qd on im.id=qd.InterviewId  
join InterviewComments ic on im.id=ic.InterviewId   
outer Apply (Select Count(distinct ima.id) as TotalCompletedMeetings from InterviewMeetings ima join InterviewComments ica on ica.InterviewId=ima.id where ica.MeetingStatus in (4, 6, 7, 8) and ima.IRFId=im.IRFId and im.interviewer=ima.interviewer) ica
where confirmationStatus=1 and ir.id=@irfId
and (MeetingStatus=4 or MeetingStatus=6 or MeetingStatus=7 or MeetingStatus=8)
group by im.interviewer,ica.TotalCompletedMeetings  having Count(qd.id)<5
end  
END  
 
GO