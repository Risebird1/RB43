IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_UpdateInterviewerinterest'))
BEGIN
    DROP PROCEDURE dbo.usp_UpdateInterviewerinterest
END
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
Create PROCEDURE usp_UpdateInterviewerinterest
@status bit,
@uniqueid varchar(50)
AS
BEGIN
UPdate ip set ip.IsInterest=@status From UserList ul join InterviewerProfile ip on ip.Userid=ul.id  where ul.uniqueid=@uniqueid
END
GO