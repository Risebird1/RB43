IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_MarkInterviewCodeSessionOpened'))
BEGIN
    DROP PROCEDURE dbo.usp_MarkInterviewCodeSessionOpened
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

-- First time the candidate opens the link starts the question's clock. Later opens never move it.
CREATE PROCEDURE [dbo].[usp_MarkInterviewCodeSessionOpened]
    @Token       UNIQUEIDENTIFIER,
    @CurrentDate DATETIME
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.InterviewCodeSessions
       SET OpenedOn = @CurrentDate
     WHERE Token = @Token
       AND OpenedOn IS NULL;

    SELECT OpenedOn FROM dbo.InterviewCodeSessions WHERE Token = @Token;
END
GO
