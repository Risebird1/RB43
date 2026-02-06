IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_GetRandomTypingSpeedSampleText'))
BEGIN
    DROP PROCEDURE dbo.usp_GetRandomTypingSpeedSampleText
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE usp_GetRandomTypingSpeedSampleText
AS
BEGIN
    SET NOCOUNT ON;

	-- Get a random active sample text
	SELECT TOP 1 
		id,
		SampleText,
		IsActive
	FROM TypingSpeedSampleTexts
	WHERE IsActive = 1
	ORDER BY NEWID(); -- Random selection
END;
GO
