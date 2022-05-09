IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_ClientUserDetailByIRF'))
BEGIN
    DROP PROCEDURE dbo.usp_ClientUserDetailByIRF
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create PROCEDURE [dbo].[usp_ClientUserDetailByIRF] 
  @Email varchar(200),  
  @Irfid int  
AS        
BEGIN        
Select cu.id, ul.FirstName,ul.LastName,ul.EmailId,Ul.MobileNumber,ul.idProfile,tc.IsActive as IsRecruiterActive 
From TransClientUser tc join IRF on IRF.id=tc.IRFId 
join UserList ul on ul.id=tc.ClientUserId  
join ClientUser cu on ul.id=cu.UserId 
Where ul.EmailId=@Email and IRF.id=@Irfid        
END   
  

Go