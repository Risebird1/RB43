  IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_SurveyInterviewerDetails'))
BEGIN
    DROP PROCEDURE [dbo].[usp_SurveyInterviewerDetails]
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
    
CREATE PROCEDURE [dbo].[usp_SurveyInterviewerDetails]
(
      @SurveyBulkDetails AS [dbo].[CuratedIRFMapping_TableType] READONLY
)
AS
BEGIN

SET NOCOUNT ON;

Select id as Interviewid,EmailId as EmailAddress from UserList im join @SurveyBulkDetails sb on sb.CuratedGroupId=im.id

END
GO