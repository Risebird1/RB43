IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_UpdateInterviewerAcknowledgementStatus'))
BEGIN
    DROP PROCEDURE dbo.usp_UpdateInterviewerAcknowledgementStatus
END
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
Create PROCEDURE usp_UpdateInterviewerAcknowledgementStatus
@value varchar(5),
@timeNow datetime,
@PerformedBy varchar(150)
AS
BEGIN
Declare @Userid int
Select @Userid=id From UserList where Emailid=@PerformedBy
Update InterviewerProfile set AcknowledgementStatus=@value where Userid=@Userid
END
GO