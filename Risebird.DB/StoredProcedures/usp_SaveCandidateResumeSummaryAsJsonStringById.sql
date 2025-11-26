/****** Object:  StoredProcedure [dbo].[usp_GetCandidateResumeSummaryById]  ******/
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_SaveCandidateResumeSummaryAsJsonStringById'))
BEGIN
    DROP PROCEDURE dbo.usp_SaveCandidateResumeSummaryAsJsonStringById
END
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE[dbo].[usp_SaveCandidateResumeSummaryAsJsonStringById]
@candidateId int,
@resumeSummaryAsJsonString varchar(max)
AS
Begin        
      
Update Candidate set ResumeSummary = @resumeSummaryAsJsonString where id = @candidateId
      
End 

