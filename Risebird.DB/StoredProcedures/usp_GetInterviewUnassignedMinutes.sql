IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetInterviewUnassignedMinutes'))
BEGIN
    DROP PROCEDURE dbo.usp_GetInterviewUnassignedMinutes
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_GetInterviewUnassignedMinutes]
AS
BEGIN
    SET NOCOUNT ON;

    SELECT InterviewerUnassignedMinutes
    FROM Settings
END
GO
