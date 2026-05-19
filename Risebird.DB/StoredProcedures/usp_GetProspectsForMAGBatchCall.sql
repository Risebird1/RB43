IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'usp_GetProspectsForMAGBatchCall')
    DROP PROCEDURE usp_GetProspectsForMAGBatchCall
GO

CREATE PROCEDURE [dbo].[usp_GetProspectsForMAGBatchCall]
    @MagName      NVARCHAR(200),
    @OnlyUncalled BIT = 1
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        Id, CandidateName, ParsedEmail, ParsedPhone, MagName, KeySkills,
        ExperienceYears, PlivoCallStatus, PlivoProspectDisposition,
        PlivoCallAttemptsTotal, Status, Source
    FROM NaukriProspectiveInterviewer
    WHERE MagName = @MagName
      AND ParsedPhone IS NOT NULL
      AND LEN(LTRIM(RTRIM(ParsedPhone))) >= 10
      AND (@OnlyUncalled = 0 OR ISNULL(PlivoCallStatus, '') = '')
    ORDER BY ReceivedAt ASC
END
GO
