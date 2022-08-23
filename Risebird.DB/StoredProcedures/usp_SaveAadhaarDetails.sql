IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_SaveAadhaarDetails'))
BEGIN
    DROP PROCEDURE dbo.usp_SaveAadhaarDetails
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_SaveAadhaarDetails]    
@Name varchar(100),     
@Address varchar(max),  
@email varchar(200),  
@reference_id varchar(50),  
@gender varchar(1),  
@dob varchar(20),  
@doc_face varchar(max),  
@verified_using varchar(20)  
AS    
Begin   
Insert into AadhaarDetails(Name,Address,email,reference_id,gender,dob,doc_face,verified_using,CreatedOn)  
values(@Name,@Address,@email,@reference_id,@gender,@dob,@doc_face,@verified_using,Getdate())  

Update UserList set AadhaarVerified=1 where EmailId=@email
End
GO