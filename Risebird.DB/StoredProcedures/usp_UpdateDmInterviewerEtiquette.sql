IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_UpdateDmInterviewerEtiquette'))
BEGIN
    DROP PROCEDURE dbo.usp_UpdateDmInterviewerEtiquette
END
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_UpdateDmInterviewerEtiquette]
    @irfId int,
    @dressCode varchar(20),
    @cameraRequired bit,
    @noEatingRule bit
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE IRF
    SET DmEtiquetteDressCode = CASE
            WHEN LOWER(LTRIM(RTRIM(@dressCode))) = 'match' THEN 'match'
            ELSE 'formal'
        END,
        IsVideoRequired = ISNULL(@cameraRequired, 0),
        DmEtiquetteNoEatingRule = ISNULL(@noEatingRule, 1)
    WHERE id = @irfId
END
GO
