IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetQCPendingReportsForAIQC'))
BEGIN
    DROP PROCEDURE dbo.usp_GetQCPendingReportsForAIQC
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create PROCEDURE [dbo].[usp_GetQCPendingReportsForAIQC]  
@timeNow datetime
As
Begin

INSERT INTO dbo.TechSkillComments (IRFId, Comment, UserId, SkillName)  
SELECT distinct  
    irf.id,  
    --IM.IRFId,                          -- maps InterviewMeetings.IRFId ? TechSkillComments.IRFId  
    QD.Comment,                        -- maps QuestionsDiscussed.Comment ? TechSkillComments.Comment  
    UL.id AS UserId,                   -- UserList.id ? TechSkillComments.UserId  
    QD.SkillName                       -- maps QuestionsDiscussed.SkillName ? TechSkillComments.SkillName  
FROM dbo.QuestionsDiscussed QD  
INNER JOIN dbo.InterviewMeetings IM   
    ON QD.InterviewId = IM.id  
 inner join irf on irf.irfid = im.irfid  
INNER JOIN dbo.UserList UL  
    ON IM.Interviewer = UL.EmailId  
join TransTechSkill tts on (tts.SkillName = qd.SkillName and tts.irfid = irf.id )

WHERE NOT EXISTS (  
    SELECT 1  
    FROM dbo.TechSkillComments TSC  
 join IRF on irf.id = tsc.IRFId  
    WHERE irf.IRFId = IM.IRFId  
      AND ISNULL(TSC.Comment, '') = ISNULL(QD.Comment, '')  
      AND ISNULL(TSC.SkillName, '') = ISNULL(QD.SkillName, '')  
      AND TSC.UserId = UL.id  
) and cast(IM.interviewDate as date) > cast(dateadd(day, -60, getdate()) as date) 


----------


Select distinct top 2 im.id, IRF.id as irfIntId
into #TempTable from 
InterviewMeetings im join IRF on im.IRFId=irf.IRFId 
join Client cl on irf.ClientId=cl.id join InterviewComments ic on ic.InterviewId=im.id 
join UserList uli on uli.EmailId=im.candidateEmail join  CandidatesMaster cm on cm.UserId=uli.id 
join Candidate ca on ca.CandidateMasterId=cm.id and ca.IRFId=IRF.id 
--left join PaymentSLADetails tq on tq.Interviewid=im.id and ISNULL(tq.isQcDone,0)=1 
left join UserList ulco on IRF.RisebirdHr=ulco.id
Outer Apply(Select Max(Joinedon) Joinedon from  InterviewMeetingsJoinTime 
left join UserList ulco on IRF.RisebirdHr=ulco.id where im.interviewer=interviewer and im.id=interviewid) imjt 
where (ic.MeetingStatus=4 or ic.MeetingStatus=6 or ic.MeetingStatus=7 or ic.MeetingStatus=8) --IsNull(IsProctor,0)=0 and 
and interviewDate<@timeNow and DATEDIFF(DAY, interviewDate,@timeNow)<7 and ISNULL(im.RedoInterview,0)=0 --and tq.id is null 
and (ic.IsAIQCAtempted is null or ic.IsAIQCAtempted = 0) and ic.OpenAIQCResponse is null

update InterviewComments set IsAIQCAtempted = 1 where InterviewId in (select id from #TempTable)
 
select * from #TempTable

End
GO

--the following sp is currently running on the db to redo the qc analyis of the interviews that took place
--within 5 days before the mentioned datetime below. Change the sp back to the above version once 
--the background job is done with this


--alter PROCEDURE [dbo].[usp_GetQCPendingReportsForAIQC]  
--@timeNow datetime
--As
--Begin

--set @timeNow = '2025-04-23 13:01:00.610'

--Select distinct top 2 im.id, IRF.id as irfIntId
--into #TempTable from 
--InterviewMeetings im join IRF on im.IRFId=irf.IRFId 
--join Client cl on irf.ClientId=cl.id join InterviewComments ic on ic.InterviewId=im.id 
--join UserList uli on uli.EmailId=im.candidateEmail join  CandidatesMaster cm on cm.UserId=uli.id 
--join Candidate ca on ca.CandidateMasterId=cm.id and ca.IRFId=IRF.id 
--left join PaymentSLADetails tq on tq.Interviewid=im.id and ISNULL(tq.isQcDone,0)=1 
--left join UserList ulco on IRF.RisebirdHr=ulco.id
--Outer Apply(Select Max(Joinedon) Joinedon from  InterviewMeetingsJoinTime 
--left join UserList ulco on IRF.RisebirdHr=ulco.id where im.interviewer=interviewer and im.id=interviewid) imjt 
--where IsNull(IsProctor,0)=0 and (ic.MeetingStatus=4 or ic.MeetingStatus=6 or ic.MeetingStatus=7 or ic.MeetingStatus=8) 
--and tq.id is null and interviewDate<@timeNow and DATEDIFF(DAY, interviewDate,@timeNow)<5 and ISNULL(im.RedoInterview,0)=0
--and (ic.IsAIQCAtempted is null or ic.IsAIQCAtempted = 0) --and ic.OpenAIQCResponse is null

--update InterviewComments set IsAIQCAtempted = 1 where InterviewId in (select id from #TempTable)
 
--select * from #TempTable

--End