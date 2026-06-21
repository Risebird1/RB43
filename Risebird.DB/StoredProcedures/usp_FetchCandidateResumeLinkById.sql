IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_FetchCandidateResumeLinkById'))
BEGIN
    DROP PROCEDURE dbo.usp_FetchCandidateResumeLinkById
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_FetchCandidateResumeLinkById]
    @id INT
AS
BEGIN
    SELECT ResumeLink FROM CandidateResume WHERE id = @id
END
GO
