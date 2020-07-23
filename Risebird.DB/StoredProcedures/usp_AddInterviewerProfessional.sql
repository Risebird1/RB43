/****** Object:  StoredProcedure [dbo].[usp_AddInterviewerProfessional]    Script Date: 21-07-2020 3.08.27 PM ******/
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_AddInterviewerProfessional'))
BEGIN
    DROP PROCEDURE dbo.usp_AddInterviewerProfessional
END


SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_AddInterviewerProfessional]


@InterviewerProfileId int,
@CompanyName nvarchar(200),
@Type varchar(50),
@CompanyStage varchar(100),
@Size nvarchar(50),
@JobFunctionId varchar(100),
@IndustryId varchar(150),
@Headquarters varchar(100),
@Role varchar(100),
@Domain nvarchar(200),
@SeniorityLevel varchar(50),
@ExperienceId nvarchar(50),
@WorkNature varchar(50),
@PrimaryResponsibility varchar(50),
@CompanyLinkedinPage nvarchar(max),
@CompanyWebsite nvarchar(max)

AS
Begin


INSERT INTO [dbo].[TransInterviewerProfessional]
           ([InterviewerProfileId]
           ,[CompanyName]
           ,[Type]
           ,[CompanyStage]
           ,[Size]
           ,[JobFunctionId]
           ,[IndustryId]
           ,[Headquarters]
           ,[Role]
           ,[Domain]
           ,[SeniorityLevel]
           ,[ExperienceId]
           ,[WorkNature]
           ,[PrimaryResponsibility]
           ,[CompanyLinkedinPage]
           ,[CompanyWebsite])
     VALUES
           (@InterviewerProfileId,
			@CompanyName,
			@Type,
			@CompanyStage,
			@Size,
			@JobFunctionId,
			@IndustryId,
			@Headquarters,
			@Role,
			@Domain,
			@SeniorityLevel,
			@ExperienceId,
			@WorkNature,
			@PrimaryResponsibility,
			@CompanyLinkedinPage,
			@CompanyWebsite)
End
GO


