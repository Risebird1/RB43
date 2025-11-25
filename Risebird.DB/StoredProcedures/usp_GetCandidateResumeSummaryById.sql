/****** Object:  StoredProcedure [dbo].[usp_GetCandidateResumeSummaryById]  ******/
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_GetCandidateResumeSummaryById'))
BEGIN
    DROP PROCEDURE dbo.usp_GetCandidateResumeSummaryById
END
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE[dbo].[usp_GetCandidateResumeSummaryById]
@candidateId int
AS
Begin        
      
select ResumeSummary, ResumeLink from Candidate where id = @candidateId
      
End 