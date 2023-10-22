IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_FatchPaymentCompliance'))
BEGIN
    DROP PROCEDURE dbo.usp_FatchPaymentCompliance
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create Procedure usp_FatchPaymentCompliance
@id int
As
Begin
Select * From PaymentSLADetails where id=@id
End
GO