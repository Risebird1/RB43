/****** Object:  StoredProcedure [dbo].[usp_AddInterviewerEducation]    Script Date: 5/1/2020 4:00:59 PM ******/

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_AddInterviewerEducation'))
BEGIN
    DROP PROCEDURE [dbo].[usp_AddInterviewerEducation]
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO



Create PROCEDURE [dbo].[usp_AddInterviewerEducation]

@InterviewerProfileId int,
@EducationType varchar(100),
@Degree nvarchar(100),
@EducationMode varchar(150),
@CollegeType nvarchar(100),
@UniversityName nvarchar(200),
@PassOutYear int,
@AnyGap bit

AS
Begin


INSERT INTO [dbo].[TransInterviewerEducation]
           ([InterviewerProfileId]
           ,[EducationType]
           ,[Degree]
           ,[EducationMode]
           ,[CollegeType]
           ,[UniversityName]
           ,[PassOutYear]
           ,[AnyGap])
     VALUES
           (@InterviewerProfileId,
			@EducationType,
			@Degree,
			@EducationMode,
			@CollegeType,
			@UniversityName,
			@PassOutYear,
			@AnyGap)
End
GO

