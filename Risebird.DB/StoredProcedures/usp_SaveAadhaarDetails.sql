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
@verified_using varchar(20),
@matchPercentage varchar(8),
@passport_image varchar(max)
AS    
Begin 
Insert into AadhaarDetails(Name,Address,email,reference_id,gender,dob,doc_face,verified_using,CreatedOn,matchPercentage,user_image)  
values(@Name,@Address,@email,@reference_id,@gender,@dob,@doc_face,@verified_using,Getdate(),@matchPercentage,@passport_image)  

Update UserList set AadhaarVerified=1 where EmailId=@email
End
GO