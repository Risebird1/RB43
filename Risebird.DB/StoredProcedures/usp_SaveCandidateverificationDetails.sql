IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_SaveCandidateverificationDetails'))
BEGIN
    DROP PROCEDURE dbo.usp_SaveCandidateverificationDetails
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[usp_SaveCandidateverificationDetails]      
@name varchar(200),       
@address varchar(max),    
@email varchar(200),    
@passportNumber varchar(15),    
@gender varchar(2),    
@birthDate varchar(25),    
@nationality varchar(25),    
@matchPercentage varchar(8)    
AS      
Begin     
Insert into CandidateVerification(email,name,birthDate,passportNumber,address,matchPercentage,nationality,gender,CreatedOn)    
values(@email,@name,@birthDate,@passportNumber,@address,@matchPercentage,@nationality,@gender,Getdate())    
  
--Update UserList set AadhaarVerified=1 where EmailId=@email  
End
GO