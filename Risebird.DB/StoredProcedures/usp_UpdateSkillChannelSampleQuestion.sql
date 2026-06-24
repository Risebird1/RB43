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
    @expectedAnswer nvarchar(max) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE SkillChannelSampleQuestions
    SET Comment = @comment,
        ExpectedAnswer = @expectedAnswer
    WHERE id = @id;
END
GO
