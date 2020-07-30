/****** Object:  StoredProcedure [dbo].[usp_AddInterviewerSkills]    Script Date: 5/1/2020 3:59:59 PM ******/

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_AddInterviewerSkills'))
BEGIN
    DROP PROCEDURE [dbo].[usp_AddInterviewerSkills]
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO



Create PROCEDURE [dbo].[usp_AddInterviewerSkills]


@InterviewerProfileId int, 
@SkillId int, 
@Category int, 
@Ip int, 
@Level int,
@SkillExperience int,
@Nature int

AS
Begin
INSERT INTO [dbo].[TransInterviewerSkill]
		(InterviewerProfileId, 
		SkillId,  
		Category, 
		Ip, 
		Level, 
		SkillExperience, 
		Nature)
 Values 
		(@InterviewerProfileId, 
		 @SkillId,
		 @Category, 
		 @Ip, 
		 @Level,
		 @SkillExperience,
		 @Nature);

End
GO


