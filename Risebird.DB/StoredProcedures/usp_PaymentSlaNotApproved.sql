IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_PaymentSlaNotApproved'))
BEGIN
    DROP PROCEDURE dbo.usp_PaymentSlaNotApproved
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE usp_PaymentSlaNotApproved
@Interviewid int,
@createdon datetime,
@ModifiedBy varchar(150)
AS
BEGIN
Update PaymentSLADetails set IsApproved=2,ModifiedOn=@createdon,ModifiedBy=@ModifiedBy where Interviewid=@Interviewid
END
GO
