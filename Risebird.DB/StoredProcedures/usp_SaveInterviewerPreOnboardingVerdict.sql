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
declare @AIVerdict varchar(50)
if(@screeningResponseJson is not null and @screeningResponseJson != 'null' and LOWER(@screeningResponseJson) LIKE '%verdict%')
Begin
		 
		SET @AIVerdict =  CASE 
		-- Check if the substring after 'verdict' (excluding 'Annotation' or 'Annotations') contains 'no', 'not', or 'doesn't'
		WHEN LOWER(REPLACE(REPLACE(SUBSTRING(@screeningResponseJson, CHARINDEX('verdict', LOWER(@screeningResponseJson)) + 7, 50), 'annotation', ''), 'annotations', '')) LIKE '%no%' OR
			 LOWER(REPLACE(REPLACE(SUBSTRING(@screeningResponseJson, CHARINDEX('verdict', LOWER(@screeningResponseJson)) + 7, 50), 'annotation', ''), 'annotations', '')) LIKE '%not%' OR
			 LOWER(REPLACE(REPLACE(SUBSTRING(@screeningResponseJson, CHARINDEX('verdict', LOWER(@screeningResponseJson)) + 7, 50), 'annotation', ''), 'annotations', '')) LIKE '%doesn''t%'
		THEN 'No'
		ELSE 'Yes'
		End
			  
End

if not exists (select top 1 id from PreOnboardingInterviewerAIVerdict where EmailId = @EmailId)
Begin
	insert into PreOnboardingInterviewerAIVerdict(EmailId, PrimarySpecialization, OpenAIResponse, AIVerdict, CreatedAt, CreatedBy)
	values (@EmailId, @PrimarySpecialization, @screeningResponseJson, @AIVerdict, @timeNow, @PerformedBy)
End
else
Begin
	update PreOnboardingInterviewerAIVerdict set OpenAIResponse = @screeningResponseJson, AIVerdict =@AIVerdict, ModifiedBy =@PerformedBy, ModifiedAt =@timeNow where EmailId = @EmailId and PrimarySpecialization = @PrimarySpecialization
End

End
GO