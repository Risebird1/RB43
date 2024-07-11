IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_SaveTransInterviewerOpenAIScreeningResults'))
BEGIN
    DROP PROCEDURE dbo.usp_SaveTransInterviewerOpenAIScreeningResults
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create PROCEDURE [dbo].[usp_SaveTransInterviewerOpenAIScreeningResults]       
@transInterviewerId int,  
@screeningResponseJson varchar(max)  
As  
Begin  
  
if(@screeningResponseJson is not null and @screeningResponseJson != 'null' and LOWER(@screeningResponseJson) LIKE '%verdict%')
Begin
	  update TransInterviewer set OpenAIScreeningResponse = @screeningResponseJson where id = @transInterviewerId 


	  UPDATE TransInterviewer
		SET AIVerdict = CASE 
		-- Check if the substring after 'verdict' (excluding 'Annotation' or 'Annotations') contains 'no', 'not', or 'doesn't'
		WHEN LOWER(REPLACE(REPLACE(SUBSTRING(OpenAIScreeningResponse, CHARINDEX('verdict', LOWER(OpenAIScreeningResponse)) + 7, 50), 'annotation', ''), 'annotations', '')) LIKE '%no%' OR
			 LOWER(REPLACE(REPLACE(SUBSTRING(OpenAIScreeningResponse, CHARINDEX('verdict', LOWER(OpenAIScreeningResponse)) + 7, 50), 'annotation', ''), 'annotations', '')) LIKE '%not%' OR
			 LOWER(REPLACE(REPLACE(SUBSTRING(OpenAIScreeningResponse, CHARINDEX('verdict', LOWER(OpenAIScreeningResponse)) + 7, 50), 'annotation', ''), 'annotations', '')) LIKE '%doesn''t%'
		THEN 'No'
		ELSE 'Yes'

		END
		WHERE id = @transInterviewerId  
End  
  
End  