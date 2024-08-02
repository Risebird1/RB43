IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_SaveOpenAIReportQC'))
BEGIN
    DROP PROCEDURE dbo.usp_SaveOpenAIReportQC
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create PROCEDURE [dbo].[usp_SaveOpenAIReportQC]     
@interviewId int,
@openAIQcResponseJson varchar(max)
As
Begin

if(@openAIQcResponseJson is not null and @openAIQcResponseJson != 'null' )
Begin
	  update InterviewComments set OpenAIQCResponse = @openAIQcResponseJson where InterviewId = @interviewId

	  UPDATE InterviewComments
		SET AIVerdict = CASE 
		-- Check if the substring after 'verdict' (excluding 'Annotation' or 'Annotations') contains 'no', 'not', or 'doesn't'
		WHEN LOWER(REPLACE(REPLACE(SUBSTRING(OpenAIQCResponse, CHARINDEX('verdict', LOWER(OpenAIQCResponse)) + 7, 50), 'annotation', ''), 'annotations', '')) LIKE '%no%' OR
 LOWER(REPLACE(REPLACE(SUBSTRING(OpenAIQCResponse, CHARINDEX('verdict', LOWER(OpenAIQCResponse)) + 7, 50), 'annotation', ''), 'annotations', '')) LIKE '%not%' OR
 LOWER(REPLACE(REPLACE(SUBSTRING(OpenAIQCResponse, CHARINDEX('verdict', LOWER(OpenAIQCResponse)) + 7, 50), 'annotation', ''), 'annotations', '')) LIKE '%doesn''t%'
		THEN 'No'
		ELSE 'Yes'

		END
		WHERE InterviewId = @interviewId

End

End
GO