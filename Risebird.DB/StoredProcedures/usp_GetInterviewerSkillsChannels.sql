IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetInterviewerSkillsChannels'))
BEGIN
    DROP PROCEDURE dbo.usp_GetInterviewerSkillsChannels
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_GetInterviewerSkillsChannels]
AS
BEGIN
    SET NOCOUNT ON;

    SELECT *
    FROM InterviewerSkillsChannel
    WHERE IsDeleted = 0
END
GO
