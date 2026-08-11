IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_UpdateSkillChannelSampleQuestion'))
BEGIN
    DROP PROCEDURE dbo.usp_UpdateSkillChannelSampleQuestion
END
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_UpdateSkillChannelSampleQuestion]
    @id int,
    @comment nvarchar(max),
    @expectedAnswer nvarchar(max) = NULL,
    @isCodingQuestion bit = 0,
    @codingTimeLimitInMinutes int = NULL
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE SkillChannelSampleQuestions
    SET Comment = @comment,
        ExpectedAnswer = @expectedAnswer,
        IsCodingQuestion = ISNULL(@isCodingQuestion, 0),
        CodingTimeLimitInMinutes = CASE WHEN ISNULL(@isCodingQuestion, 0) = 1 THEN @codingTimeLimitInMinutes ELSE NULL END
    WHERE id = @id;
END
GO
