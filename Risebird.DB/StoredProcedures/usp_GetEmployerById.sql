/****** Object:  StoredProcedure [dbo].[usp_GetEmployerById]    Script Date: 5/1/2020 4:04:19 PM ******/

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetEmployerById'))
BEGIN
    DROP PROCEDURE [dbo].[usp_GetEmployerById]
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO




CREATE PROCEDURE [dbo].[usp_GetEmployerById]

@id int

AS
Begin

SELECT [Id], [InterviewerProfileId], [CompanyName],[Type], [CompanyStage], [Size], [JobFunctionId], [IndustryId],
[Headquarters], [Role], [Domain], [SeniorityLevel], [ExperienceId], [WorkNature], [PrimaryResponsibility],
[CompanyLinkedinPage], [CompanyWebsite] FROM[dbo].[TransInterviewerProfessional] where Id = @id

End



GO

