  IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_AddInterviewer'))
BEGIN
    DROP PROCEDURE [dbo].[usp_AddInterviewer]
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
    
      
CREATE PROCEDURE [dbo].[usp_AddInterviewer]        
@selectedSkills varchar(max),        
@resumeLink varchar(max),        
@slots varchar(500),    
@userFirstName varchar(150),        
@userLastName varchar(150),        
@userEmailId varchar(150),        
@userMobileNumber nvarchar(200),        
@isApproved bit,  
@Heuristicsscore int        
AS        
Begin        
        
declare @generatedUserId int;        
        
Begin Transaction        
        
  Begin Try        
           
    INSERT INTO [dbo].[UserList]        
         (        
       [idProfile]        
         ,[FirstName]             
         ,[LastName]        
         ,[EmailId]        
         ,[IsActive]        
         ,[MobileNumber]   
   ,[uniqueid]  
         )        
        VALUES        
         (        
       3        
         ,@userFirstName        
         ,@userLastName        
         ,@userEmailId        
         ,1        
         ,@userMobileNumber,  
   NEWID()  
         )        
        
    set @generatedUserId = SCOPE_IDENTITY();        
        
    INSERT INTO [dbo].[Interviewer]        
         (               
          [UserId]             
         ,[PrimarySkill]        
         ,[ResumeLink]        
         ,[isApproved]        
             
         ,[Slots] ,[Heuristicsscore]       
                  
         )        
        VALUES        
         (               
           @generatedUserId         
       ,@selectedSkills        
       ,@resumeLink        
       ,1        
       
       ,@slots ,@Heuristicsscore       
      
         )        
    Commit;        
        
  End Try        
          
  Begin Catch        
          
   RAISERROR ('Error While Data Insertion ClientUser', -- Message text.        
       16, -- Severity.        
       1 -- State.        
       );        
        
   Rollback;        
        
  End Catch        
        
End 
GO