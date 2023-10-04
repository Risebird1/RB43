IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_updateRedmarkerAcknowledge'))
BEGIN
    DROP PROCEDURE usp_updateRedmarkerAcknowledge
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create Procedure usp_updateRedmarkerAcknowledge
@CurrentDate datetime,
@uniqueid varchar(17)
AS
BEGIN
Update ip set RedmarkerAcknowledge=1 from InterviewerProfile ip join UserList ul on ul.id=ip.Userid where ul.uniqueid=@uniqueid
END
GO