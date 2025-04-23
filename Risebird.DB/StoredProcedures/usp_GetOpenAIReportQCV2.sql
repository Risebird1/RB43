IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetOpenAIReportQCV2'))
BEGIN
    DROP PROCEDURE dbo.usp_GetOpenAIReportQCV2
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create PROCEDURE [dbo].[usp_GetOpenAIReportQCV2]     
@interviewId int
As
Begin
 select top 1 OpenAIQCResponse as completeAnalysis, AIVerdict as verdict, AIReportQCAnalysisScore as score from InterviewComments where InterviewId = @interviewId
End
GO

