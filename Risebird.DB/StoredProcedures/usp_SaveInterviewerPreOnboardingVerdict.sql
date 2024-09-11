IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_SaveInterviewerPreOnboardingVerdict'))
BEGIN
    DROP PROCEDURE dbo.usp_SaveInterviewerPreOnboardingVerdict
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create PROCEDURE [dbo].[usp_SaveInterviewerPreOnboardingVerdict]     
@EmailId [varchar](50),
@PrimarySpecialization [varchar](250),
@screeningResponseJson varchar(max),
@PerformedBy varchar(200),
@timeNow datetime
As
Begin

if(@screeningResponseJson is not null and @screeningResponseJson != 'null' and LOWER(@screeningResponseJson) LIKE '%verdict%')
Begin
		declare @AIVerdict varchar(50) 
		SET @AIVerdict =  CASE 
		-- Check if the substring after 'verdict' (excluding 'Annotation' or 'Annotations') contains 'no', 'not', or 'doesn't'
		WHEN LOWER(REPLACE(REPLACE(SUBSTRING(@screeningResponseJson, CHARINDEX('verdict', LOWER(@screeningResponseJson)) + 7, 50), 'annotation', ''), 'annotations', '')) LIKE '%no%' OR
			 LOWER(REPLACE(REPLACE(SUBSTRING(@screeningResponseJson, CHARINDEX('verdict', LOWER(@screeningResponseJson)) + 7, 50), 'annotation', ''), 'annotations', '')) LIKE '%not%' OR
			 LOWER(REPLACE(REPLACE(SUBSTRING(@screeningResponseJson, CHARINDEX('verdict', LOWER(@screeningResponseJson)) + 7, 50), 'annotation', ''), 'annotations', '')) LIKE '%doesn''t%'
		THEN 'No'
		ELSE 'Yes'
		End

		if not exists (select top 1 id from PreOnboardingInterviewerAIVerdict where EmailId = @EmailId and PrimarySpecialization = @PrimarySpecialization)
		Begin
			insert into PreOnboardingInterviewerAIVerdict(EmailId, PrimarySpecialization, OpenAIResponse, AIVerdict, CreatedAt, CreatedBy)
			values (@EmailId, @PrimarySpecialization, @screeningResponseJson, @AIVerdict, @timeNow, @PerformedBy)
		End
	  
End

End
GO