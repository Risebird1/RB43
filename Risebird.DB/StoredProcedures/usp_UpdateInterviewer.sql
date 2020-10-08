  IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_UpdateInterviewer'))
BEGIN
    DROP PROCEDURE dbo.usp_UpdateInterviewer
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

      
Create PROCEDURE [dbo].[usp_UpdateInterviewer](@id INT,       
@selectedSkills varchar(max),      
@resumeLink varchar(max),   
@isApproved bit,  
      
@slots varchar(500),      
      
@userFirstName varchar(150),      
@userLastName varchar(150),      
@userEmailId varchar(150),      
@userMobileNumber nvarchar(200),
@Heuristicsscore int )      
AS      
Begin Try      
  BEGIN TRAN UpdateInterviewer       
             
      
       IF EXISTS( SELECT 1 FROM UserList ul JOIN dbo.Interviewer i on ul.id = i.UserId where i.id = @id )      
       BEGIN      
             UPDATE ul SET FirstName =  @userFirstName,       
                                 LastName =  @userLastName,      
                              EmailID =   @userEmailId,      
         MobileNumber = @userMobileNumber      
         FROM UserList ul JOIN dbo.Interviewer i on ul.id = i.UserId where i.id = @id      
      
   UPDATE Interviewer SET PrimarySkill = @selectedSkills, ResumeLink = @resumeLink, IsApproved=@isApproved,      
       Slots = @slots ,Heuristicsscore=@Heuristicsscore    
         where id = @id       
      
                          
       END      
 Commit TRAN UpdateInterviewer;      
End Try       
Begin Catch      
 RAISERROR ('Error While Updating Interviewer Details', -- Message text.      
   16, -- Severity.      
   1 -- State.      
   );      
      
 Rollback;      
End Catch      
      
      
      
      
      