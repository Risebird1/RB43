/****** Object:  StoredProcedure [dbo].[usp_EditInterviewerProfessional]    Script Date: 5/1/2020 4:04:48 PM ******/

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_EditInterviewerProfessional'))
BEGIN
    DROP PROCEDURE [dbo].[usp_EditInterviewerProfessional]
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO




Create PROCEDURE [dbo].[usp_EditInterviewerProfessional]

@Id int,
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


Update  [dbo].[TransInterviewerProfessional]

	SET
           
           [CompanyName] = @CompanyName
           ,[Type] = @Type
           ,[CompanyStage] = @CompanyStage
           ,[Size] = @Size
           ,[JobFunctionId] = @JobFunctionId
           ,[IndustryId] = @IndustryId
           ,[Headquarters] = @Headquarters
           ,[Role] = @Role
           ,[Domain] = @Domain
           ,[SeniorityLevel] = @SeniorityLevel
           ,[ExperienceId] = @ExperienceId
           ,[WorkNature] = @WorkNature
           ,[PrimaryResponsibility] = @PrimaryResponsibility
           ,[CompanyLinkedinPage] = @CompanyLinkedinPage
           ,[CompanyWebsite] = @CompanyWebsite
		   
     WHERE Id = @Id and InterviewerProfileId = @InterviewerProfileId;
End
GO

