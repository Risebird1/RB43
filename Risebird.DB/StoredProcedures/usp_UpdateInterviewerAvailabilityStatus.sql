IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_UpdateInterviewerAvailabilityStatus'))
BEGIN
    DROP PROCEDURE dbo.usp_UpdateInterviewerAvailabilityStatus
END
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
Create PROCEDURE usp_UpdateInterviewerAvailabilityStatus
@userId int,
@IsAvailable bit,
@irfId int,
@timeNow datetime,
@PerformedBy varchar(150)
AS
BEGIN

Update TransInterviewer set IsAvailable =@IsAvailable, ModifiedBy = @PerformedBy, ModifiedAt = @timeNow
where IRFId= @irfId and UserId = @userId

END
Go


