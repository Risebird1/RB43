IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_SaveEnquiryDetails'))
BEGIN
    DROP PROCEDURE dbo.usp_SaveEnquiryDetails
END
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE Procedure usp_SaveEnquiryDetails    
@Name varchar(150),    
@Emailid varchar(200),    
@Designation varchar(150),    
@Phone varchar(20),    
@Company varchar(150),
@RequestType varchar(50)
AS    
BEGIN    
Insert into tblEnquiry (Name,Emailid,Designation,Phone,Company,CreatedOn,RequestType) values(@Name,@Emailid,@Designation,@Phone,@Company,Getdate(),@RequestType)    
END  
GO