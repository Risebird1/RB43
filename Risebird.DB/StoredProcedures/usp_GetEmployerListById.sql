/****** Object:  StoredProcedure [dbo].[usp_GetEmployerListById]    Script Date: 5/1/2020 4:03:53 PM ******/

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetEmployerListById'))
BEGIN
    DROP PROCEDURE [dbo].[usp_GetEmployerListById]
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO




CREATE PROCEDURE [dbo].[usp_GetEmployerListById]

@IProfileId int

AS
Begin

SELECT [Id], [InterviewerProfileId], [CompanyName],[Type], [CompanyStage], [Size], [JobFunctionId], [IndustryId],
[Headquarters], [Role], [Domain], [SeniorityLevel], [ExperienceId], [WorkNature], [PrimaryResponsibility],
[CompanyLinkedinPage], [CompanyWebsite] from [TransInterviewerProfessional] where InterviewerProfileId = @IProfileId
End



GO

