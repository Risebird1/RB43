-- Normalizes a MAG / Assessment Group name for tolerant equality matching.
--
-- MAG names that arrive from the Naukri/relay webhooks (and that an operator sees on screen) can
-- carry invisible characters that make a plain LTRIM/RTRIM/LOWER comparison fail even though the
-- text looks identical: non-breaking spaces (NCHAR(160)), tabs/newlines, zero-width spaces, and
-- doubled internal spaces. This function folds all of those away so that, e.g.,
-- 'Salesforce  Marketing Cloud' (double space / nbsp) matches the canonical
-- 'Salesforce Marketing Cloud' stored in MasterAssessmentGroup.
CREATE OR ALTER FUNCTION [dbo].[fn_NormalizeMagName] (@name NVARCHAR(400))
RETURNS NVARCHAR(400)
AS
BEGIN
    IF @name IS NULL
        RETURN NULL;

    DECLARE @s NVARCHAR(400) = @name;

    -- Fold odd whitespace / invisible characters into a regular space (or remove zero-width ones).
    SET @s = REPLACE(@s, NCHAR(160),  N' ');  -- non-breaking space
    SET @s = REPLACE(@s, NCHAR(9),    N' ');  -- tab
    SET @s = REPLACE(@s, NCHAR(10),   N' ');  -- line feed
    SET @s = REPLACE(@s, NCHAR(13),   N' ');  -- carriage return
    SET @s = REPLACE(@s, NCHAR(8203), N'');   -- zero-width space
    SET @s = REPLACE(@s, NCHAR(65279),N'');   -- BOM / zero-width no-break space

    -- Collapse any run of spaces to a single space in one pass (no WHILE loop). The two marker
    -- characters (NCHAR(1)/NCHAR(2)) are control chars that never appear in real MAG names.
    SET @s = REPLACE(REPLACE(REPLACE(@s, N' ', NCHAR(1) + NCHAR(2)), NCHAR(2) + NCHAR(1), N''), NCHAR(1) + NCHAR(2), N' ');

    RETURN LTRIM(RTRIM(LOWER(@s)));
END
GO
