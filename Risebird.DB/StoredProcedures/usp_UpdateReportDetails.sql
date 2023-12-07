IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_UpdateReportDetails'))
BEGIN
    DROP PROCEDURE dbo.usp_UpdateReportDetails
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE usp_UpdateReportDetails
@interviewId int,
@comment varchar(max),
@emailid varchar(100)
AS
BEGIN
update InterviewComments set InterviewerComment=ISNull(@comment,InterviewerComment),ModifiedBy=@emailid,
ModifiedOn=dateadd(MINUTE, 330, getdate()) where interviewid=@interviewId
END
GO