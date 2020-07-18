/****** Object:  StoredProcedure [dbo].[usp_AddInterviewerProjects]    Script Date: 5/1/2020 4:01:28 PM ******/

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_AddInterviewerProjects'))
BEGIN
    DROP PROCEDURE [dbo].[usp_AddInterviewerProjects]
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO




Create PROCEDURE [dbo].[usp_AddInterviewerProjects]

@InterviewerProfileId int,
@TechnologyEnvironment nvarchar(200),
@IsPrimarySkillUsed bit,
@Description nvarchar(max),
@IsCommercialProject bit,
@NatureOfProject varchar(50)

AS
Begin
INSERT INTO [dbo].[TransInterviewerProjects]
           ([InterviewerProfileId]
           ,[TechnologyEnvironment]
           ,[IsPrimarySkillUsed]
           ,[Description]
           ,[IsCommercialProject]
           ,[NatureOfProject])
     VALUES
           (@InterviewerProfileId,
			@TechnologyEnvironment,
			@IsPrimarySkillUsed,
			@Description,
			@IsCommercialProject,
			@NatureOfProject)

End
GO

