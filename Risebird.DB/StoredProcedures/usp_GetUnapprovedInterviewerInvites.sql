/****** Object:  StoredProcedure [dbo].[usp_GetUnapprovedInterviewerInvites]    Script Date: 25-05-2020 10.48.45 AM ******/
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetUnapprovedInterviewerInvites'))
BEGIN
    DROP PROCEDURE dbo.usp_GetUnapprovedInterviewerInvites
END
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_GetUnapprovedInterviewerInvites]

@emailId varchar(200)

AS
Begin

SELECT isc.InterviewerSkillId, isc.JDTiltle, isc.StartDate, isc.EndDate, isc.CalendlyLink, Skill.[Name] as PrimarySkill from InterviewerSkillsChannel isc
join TransInterviewerListInterviewerSkills tilis on isc.id = tilis.ISCId
join UserList ul on ul.id = tilis.UserId 
join Skill on isc.PrimarySkill = Skill.id
where ul.EmailId = @emailId

End



GO


