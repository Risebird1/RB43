IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_GetCandidateResumeForParsing'))
BEGIN
    DROP PROCEDURE dbo.usp_GetCandidateResumeForParsing
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
 
CREATE PROCEDURE usp_GetCandidateResumeForParsing
AS
BEGIN

select top 10 id, IRFId, ResumeLink, AddedBy from CandidateResume where IsResumeParsed = 0 and IsProcessed = 0

END   

GO

