IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_UpdateInterviewerSurcharge'))
BEGIN
    DROP PROCEDURE dbo.usp_UpdateInterviewerSurcharge
END
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
Create PROCEDURE usp_UpdateInterviewerSurcharge
@userId int,
@surcharge int,
@irfId int,
@timeNow datetime,
@PerformedBy varchar(150)
AS
BEGIN

Update TransInterviewer set Surcharge =@surcharge, ModifiedBy = @PerformedBy, ModifiedAt = @timeNow
where IRFId= @irfId and UserId = @userId

END
Go


