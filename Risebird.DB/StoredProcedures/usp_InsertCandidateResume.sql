-- usp_InsertCandidateResume
-- Inserts a single resume row into CandidateResume.
-- IsProcessed is set to 0 so the existing parser picks it up automatically.
CREATE OR ALTER PROCEDURE [dbo].[usp_InsertCandidateResume]
    @IRFId           INT,
    @ResumeLink      NVARCHAR(500),
    @AddedBy         INT,
    @ClientHREmailId VARCHAR(100) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF @IRFId IS NULL OR @AddedBy IS NULL
       OR @ResumeLink IS NULL OR LEN(LTRIM(RTRIM(@ResumeLink))) = 0
    BEGIN
        SELECT 0 AS NewId;
        RETURN;
    END

    -- Avoid duplicate entries for the same IRF + file within a short window
    IF EXISTS (
        SELECT 1 FROM dbo.CandidateResume
        WHERE  IRFId      = @IRFId
          AND  ResumeLink = @ResumeLink
          AND  AddedOn    >= DATEADD(MINUTE, -5, GETUTCDATE())
    )
    BEGIN
        SELECT 0 AS NewId;
        RETURN;
    END

    INSERT INTO dbo.CandidateResume
        (IRFId, ResumeLink, AddedBy, AddedOn, IsProcessed, ClientHREmailId)
    VALUES
        (@IRFId, @ResumeLink, @AddedBy, GETUTCDATE(), 0, @ClientHREmailId);

    SELECT CAST(SCOPE_IDENTITY() AS INT) AS NewId;
END
GO
