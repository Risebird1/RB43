/****** Object:  StoredProcedure [dbo].[usp_GetAccounts]    Script Date: 24-03-2020 8.20.45 PM ******/

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_UpdateAmountForNewlyAddedInterviewers'))
BEGIN
    DROP PROCEDURE dbo.usp_UpdateAmountForNewlyAddedInterviewers
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
  
  
CREATE PROCEDURE [dbo].[usp_UpdateAmountForNewlyAddedInterviewers]    
@userId int,  
@IRFId int,  
@PerSuccessRate int,  
@Type varchar(30)  
  
AS    
Begin   
 
 DECLARE @IDVar INT;
    
 DECLARE @tbl TABLE (id INT);
 INSERT @tbl (id)
 Select im.id from  InterviewPoints ipnt   
 join InterviewMeetings im on im.id=ipnt.Interviewid   
 join IRF on im.IRFId = irf.IRFId  
 join InterviewComments ic on ic.InterviewId=im.id  
 where irf.id=@IRFId and ipnt.userid=@userId and (ipnt.[Status]='Pending'or ipnt.[Status]='Submitted') and ipnt.[Type] ='Interview'  
    
 DECLARE @RowCount INT = (SELECT COUNT(*) FROM @tbl);
 
 WHILE @RowCount > 0 BEGIN
  SELECT @IDVar=id
  FROM @tbl 
  ORDER BY ID DESC OFFSET @RowCount - 1 ROWS FETCH NEXT 1 ROWS ONLY;
  
 Exec usp_InsertOrUpdateInterviewPoints @IDVar
    
    SET @RowCount -= 1;
 END 
  
End  
GO


