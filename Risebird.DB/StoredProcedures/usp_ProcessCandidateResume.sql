IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_ProcessCandidateResume'))
BEGIN
    DROP PROCEDURE dbo.usp_ProcessCandidateResume
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_ProcessCandidateResume]
    @id INT
AS
BEGIN
    UPDATE CandidateResume SET IsProcessed = 1 WHERE id = @id
END
GO
