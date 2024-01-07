IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_FetchSkillChannelCandidateReprots'))
BEGIN
    DROP PROCEDURE dbo.usp_FetchSkillChannelCandidateReprots
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create Procedure usp_FetchSkillChannelCandidateReprots
@irfId int
AS
BEGIN
Select im.id,im.IRFId,im.interviewDate,im.candidateEmail,im.candidateName,im.MobileNumber,IRF.JDTiltle,ic.MeetingStatus,ic.InterviewStatus,ic.InterviewerComment,im.ClientHrContact,ic.VideoLink 
from InterviewMeetings im join IRF on IRF.IRFId=im.IRFId 
join InterviewComments ic on ic.InterviewId=im.id
where IRF.id=@irfId and im.confirmationStatus=1 and (ic.MeetingStatus=4 or ic.MeetingStatus=6 or ic.MeetingStatus=7 or ic.MeetingStatus=8)

select a.*, Skill.[Name] as SkillName from 
(select ts.*, [LevelTech].[Name] as RequiredLevel from TechSkillReport ts inner join [LevelTech] on ts.RequiredLevelId=[LevelTech].id) as a inner join Skill on a.SkillId = Skill.id 
join InterviewMeetings im on im.id=a.InterviewId
join IRF on IRF.IRFId=im.IRFId
where IRF.id = @irfId

END
GO