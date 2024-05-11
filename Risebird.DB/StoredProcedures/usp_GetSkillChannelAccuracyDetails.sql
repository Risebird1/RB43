IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_GetSkillChannelAccuracyDetails'))
BEGIN
    DROP PROCEDURE dbo.usp_GetSkillChannelAccuracyDetails
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create Procedure usp_GetSkillChannelAccuracyDetails
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
select im.interviewer,count(distinct im.id) totalnegrating from IRF ir
join InterviewMeetings im on im.IRFId=ir.IRFId
join CandidateFeedbackDetails cd on cd.InterviewId=im.id
where cd.Rating<4 and ir.id=@irfId
group by im.interviewer
end
END
GO