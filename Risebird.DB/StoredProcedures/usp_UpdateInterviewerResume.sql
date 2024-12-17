IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_UpdateInterviewerResume'))
BEGIN
    DROP PROCEDURE dbo.usp_UpdateInterviewerResume
END
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
Create PROCEDURE usp_UpdateInterviewerResume
@userId int,
@ResumeLink varchar(1000),
@timeNow datetime,
@PerformedBy varchar(150)
AS
BEGIN

Update ipr set ipr.ResumeLink = @ResumeLink, ModifiedOn = @timeNow, ModifiedBy = @PerformedBy
From InterviewerProfile ipr where ipr.Userid=@userId

END
Go