IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetInterviewProxyReport'))
BEGIN
    DROP PROCEDURE dbo.usp_GetInterviewProxyReport
END
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_GetInterviewProxyReport]
    @InterviewId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT * FROM dbo.ProxyReport WHERE InterviewId = @InterviewId;
END
GO
