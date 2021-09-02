IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_InsertPaymentHistory'))
BEGIN
    DROP PROCEDURE dbo.usp_InsertPaymentHistory
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create Procedure usp_InsertPaymentHistory
@userid int,
@PaymentDate datetime,
@Amount int,
@status varchar(10),
@Comment varchar(100),
@TotalAmount int
AS
BEGIN
Insert Into PaymentHistory (userid, PaymentDate,Amount,Status,Comment,TotalAmount) Values 
(@userid, @PaymentDate, @Amount, @status,@Comment,@TotalAmount)

Select SCOPE_IDENTITY()
END
GO