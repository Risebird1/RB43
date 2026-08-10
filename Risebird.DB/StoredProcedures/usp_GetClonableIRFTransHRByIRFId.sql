IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetClonableIRFTransHRByIRFId'))
BEGIN
    DROP PROCEDURE dbo.usp_GetClonableIRFTransHRByIRFId
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_GetClonableIRFTransHRByIRFId]
    @irfId INT
AS
BEGIN
    SELECT thr.*, HR.UserId
    FROM TransClonableIRFHR thr
    INNER JOIN HR ON thr.HRId = HR.id
    WHERE ClonableIRFId = @irfId
END
GO
