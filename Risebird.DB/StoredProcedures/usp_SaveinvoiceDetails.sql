  IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_SaveinvoiceDetails'))
BEGIN
    DROP PROCEDURE [dbo].[usp_SaveinvoiceDetails]
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create Procedure usp_SaveinvoiceDetails
@id int,
@ClientName varchar(50),
@JobCountry varchar(200),
@StartDate datetime,
@Enddate datetime,
@AccountNumber varchar(20),
@AccountDetails varchar(200),
@Comment varchar(200),
@PaymentStatus int,
@ReferenceNumber varchar(50),
@UploadedFiles varchar(max),
@Price int
AS
BEGIN
Declare @Clientid int
Select @Clientid=id from Client where Name=@ClientName
if Not Exists(Select 1 from InvoiceDetails where id=@id)
BEGIN
Insert into InvoiceDetails(Clientid,JobCountry,StartDate,Enddate,AccountNumber,AccountDetails,Comment,PaymentStatus,ReferenceNumber,UploadedFiles,Price)
values(@Clientid,@JobCountry,@StartDate,@Enddate,@AccountNumber,@AccountDetails,@Comment,@PaymentStatus,@ReferenceNumber,@UploadedFiles,@Price)
END
ELSE
BEGIN
Update InvoiceDetails Set AccountNumber=ISNULL(@AccountNumber,AccountNumber),
AccountDetails=ISNULL(@AccountDetails,AccountDetails),
Comment=ISNULL(@Comment,Comment),
PaymentStatus=ISNULL(@PaymentStatus,PaymentStatus),
ReferenceNumber=ISNULL(@ReferenceNumber,ReferenceNumber),
UploadedFiles=ISNULL(@UploadedFiles,UploadedFiles),
Price=ISNULL(@Price,Price)
END

END