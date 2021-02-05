/****** Object:  StoredProcedure [dbo].[usp_GetInterviewerSkillsChannelInterviewers]    Script Date: 25-05-2020 10.57.31 AM ******/
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetInterviewerSkillsChannelInterviewers'))
BEGIN
    DROP PROCEDURE dbo.usp_GetInterviewerSkillsChannelInterviewers
END
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO




CREATE PROCEDURE [dbo].[usp_GetInterviewerSkillsChannelInterviewers] 
  @ISCId int
AS
BEGIN

select ipr.id as InterviewerProfileId, ipr.UserId, ipr.ResumeLink, ipr.CurrentRole, ipr.PreferredDomain, ul.FirstName, ul.EmailId, ul.MobileNumber, tilis.ISCId,tilis.id, ipr.Experience, ipr.City, isc.EndDate, im.id as imid from TransInterviewerListInterviewerSkills tilis 
left join InterviewerProfile ipr on ipr.id = tilis.InterviewerProfileId
left join UserList ul on ipr.UserId = ul.id
left join InterviewerSkillsChannel isc on isc.id = tilis.ISCId
left join InterviewMeetings im on (im.IRFId = isc.InterviewerSkillId and ul.EmailId=im.candidateEmail)
where tilis.ISCId = @ISCId

END
GO


