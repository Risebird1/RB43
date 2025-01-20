IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_UpdateUSTJobTitle'))
BEGIN
    DROP PROCEDURE dbo.usp_UpdateUSTJobTitle
END
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
Create PROCEDURE usp_UpdateUSTJobTitle
@agId int,
@ustJobTitle varchar(200),
@timeNow datetime,
@PerformedBy varchar(150)
AS
BEGIN

Update AssessmentGroup set USTJobTitle = @ustJobTitle
where id= @agId


END
Go


