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
@Category varchar(150), 
@Ip varchar (150), 
@Level varchar (50),
@SkillExperience nvarchar (50),
@Nature varchar (50)

AS
Begin
INSERT INTO [dbo].[TransInterviewerSkill]
		(InterviewerProfileId, 
		SkillId, 
		SkillName, 
		Category, 
		Ip, 
		Level, 
		SkillExperience, 
		Nature)
 Values 
		(@InterviewerProfileId, 
		 @SkillId, 
		 (select Description from Skill where Id = @SkillId ), 
		 @Category, 
		 @Ip, 
		 @Level,
		 @SkillExperience,
		 @Nature);

End
GO


