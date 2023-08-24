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

select top 10 id, IRFId, ResumeLink, AddedBy from CandidateResume where IsResumeParsed = 0 and IsProcessed = 0 and (IsResumeParsingFailed=0 or IsResumeParsingFailed is null) and cast(AddedOn as date) > cast('2023-08-21 03:25:23.050' as date) order by id desc

END   

GO

