IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_AgreeToPaymentSLAConditions'))
BEGIN
    DROP PROCEDURE [dbo].[usp_AgreeToPaymentSLAConditions]
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE usp_AgreeToPaymentSLAConditions
@Emailid varchar(200)
AS
BEGIN
Declare @UserId int
Select @UserId=id From UserList where Emailid=@Emailid
Update InterviewerProfile set PaymentSLAConfirmation=1 where UserId=@UserId
END
GO