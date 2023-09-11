/****** Object:  StoredProcedure [dbo].[usp_MyProfile]    Script Date: 5/2/2020 8:32:11 PM ******/
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_MyProfile'))
BEGIN
    DROP PROCEDURE [dbo].[usp_MyProfile]
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

  
Create PROCEDURE [dbo].[usp_MyProfile]      
      
@UserId int,      
@FirstName varchar(150),      
@LastName nvarchar(150),      
@EmailId varchar(150),      
@AltEmailId varchar(150),      
@MobileNumber nvarchar(200),      
@AltMobileNumber nvarchar(200),      
@ResumeLink nvarchar(max),      
@InterviewerProfileId int,    
@Isinstaavailable bit,  
@CodedResumeLink varchar(max),
@Isavailable int,
@VideoConfirmation int
      
AS      
      
If Exists (select 1 from [dbo].[InterviewerProfile] where userId = @UserId)      
-- Edit in both the tables      
Begin      
      
--Declare @v_ProfileId int;      
      
  Update [dbo].[UserList]      
  SET      
   [FirstName] = @FirstName,      
   [LastName] = @LastName,      
   [EmailId] = @EmailId,      
   [MobileNumber] = @MobileNumber       
      
   where Id = @UserId;      
      
  Update [dbo].[InterviewerProfile]      
  Set      
   [AltEmailID] = @AltEmailId,      
   [AltMobileNumber] = @AltMobileNumber,      
   [ResumeLink] = @ResumeLink,    
   [Isinstaavailable]=@Isinstaavailable,  
   [CodedResumeLink]=@CodedResumeLink,
   [Isavailable]=@Isavailable,
   [VideoConfirmation]=@VideoConfirmation 
      
 where id = @InterviewerProfileId;      
       
      
  delete from TransInterviewerAvailability where InterviewerProfileId=@InterviewerProfileId      
      
  delete from TransInterviewerAccDetails where InterviewerProfileId=@InterviewerProfileId      
      
End      
  
  
  
   




GO


