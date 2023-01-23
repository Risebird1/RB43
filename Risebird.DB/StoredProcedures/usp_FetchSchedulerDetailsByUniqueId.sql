IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_FetchSchedulerDetailsByUniqueId'))
BEGIN
    DROP PROCEDURE dbo.usp_FetchSchedulerDetailsByUniqueId
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

Create Procedure usp_FetchSchedulerDetailsByUniqueId
@uniqueid varchar(50)
AS
BEGIN
Select sie.*,IRF.MeetingPlatform,'IRF' schedulertype from ScheduleInterviewEvent sie join IRF on IRF.IRFId=sie.SkillChannel 
where uniqueid=@uniqueid and  IsActive=1 and IsNull(IsSingleUse,0)=0 
union 
Select sie.*,IRF.MeetingPlatform,'IRF' schedulertype from ScheduleInterviewEvent sie join IRF on IRF.IRFId=sie.SkillChannelRelation 
where uniqueid=@uniqueid and IsActive=1 and IsNull(IsOnetimeUsed,0)=0 and IsSingleUse=1 
union 
Select sie.*,'Zoom' MeetingPlatform,'Interviewer' schedulertype from ScheduleInterviewEvent sie join InterviewerSkillsChannel IRF on IRF.InterviewerSkillId=sie.SkillChannel 
where uniqueid=@uniqueid and  IsActive=1 and IsNull(IsSingleUse,0)=0 
union 
Select sie.*,'Zoom' MeetingPlatform,'Interviewer' schedulertype from ScheduleInterviewEvent sie join InterviewerSkillsChannel IRF on IRF.InterviewerSkillId=sie.SkillChannelRelation
where uniqueid=@uniqueid and IsActive=1 and IsNull(IsOnetimeUsed,0)=0 and IsSingleUse=1
END
GO