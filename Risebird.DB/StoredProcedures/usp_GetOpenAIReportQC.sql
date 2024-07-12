IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetOpenAIReportQC'))
BEGIN
    DROP PROCEDURE dbo.usp_GetOpenAIReportQC
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create PROCEDURE [dbo].[usp_GetOpenAIReportQC]     
@interviewId int
As
Begin
 select top 1 OpenAIQCResponse from InterviewComments where InterviewId = @interviewId
End
GO

