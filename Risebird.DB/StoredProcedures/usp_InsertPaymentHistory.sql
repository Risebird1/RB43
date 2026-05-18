IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_InsertPaymentHistory'))
BEGIN
    DROP PROCEDURE dbo.usp_InsertPaymentHistory
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE usp_InsertPaymentHistory
    @userid       INT,
    @PaymentDate  DATETIME,
    @Amount       INT,
    @status       VARCHAR(10),
    @Comment      VARCHAR(100),
    @TotalAmount  INT,
    @TransferId   NVARCHAR(50)  = NULL,
    @CfTransferId NVARCHAR(100) = NULL,
    @PayoutStatus NVARCHAR(50)  = NULL
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO PaymentHistory
        (Userid, PaymentDate, Amount, Status, Comment, TotalAmount, TransferId, CfTransferId, PayoutStatus)
    VALUES
        (@userid, @PaymentDate, @Amount, @status, @Comment, @TotalAmount, @TransferId, @CfTransferId, @PayoutStatus);

    SELECT SCOPE_IDENTITY();
END
GO
