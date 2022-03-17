/****** Object:  StoredProcedure [dbo].[usp_GetAccounts]    Script Date: 24-03-2020 8.20.45 PM ******/

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_UpdateAmountBasedOnPerSuccessRate'))
BEGIN
    DROP PROCEDURE dbo.usp_UpdateAmountBasedOnPerSuccessRate
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
  
  
  
  
CREATE PROCEDURE [dbo].[usp_UpdateAmountBasedOnPerSuccessRate]    
@userId int,  
@IRFId int,  
@percentageChange float,  
@type varchar(30)  
  
AS    
Begin   
  
if(@type='Interviewer')  
Begin  
 update InterviewPoints set Points = Round(Points + Points*@percentageChange/100, 0) where id in   
 (select distinct ipnt.id from InterviewPoints ipnt   
 join InterviewMeetings im on im.id=ipnt.Interviewid   
 join IRF on im.IRFId = irf.IRFId  
 join TransInterviewer ti on ti.IRFId = IRF.id  
 where irf.id=@IRFId and ipnt.Points>0 and ipnt.UserId = @userId and (ipnt.[Status]='Pending'or ipnt.[Status]='Submitted') and ipnt.[Type] ='Interview')  
  
  
  
End  
  
Else  
Begin  
 update InterviewPoints set Points = Round(Points + Points*@percentageChange/100, 0) where id in  
 (select distinct ipnt.id from InterviewPoints ipnt   
 join InterviewMeetings im on im.id=ipnt.Interviewid   
 join IRF on im.IRFId = irf.IRFId  
 join TransEmergencyInterviewer tei on tei.IRFId = IRF.id  
 where irf.id=@IRFId and ipnt.Points>0 and ipnt.UserId = @userId and (ipnt.[Status]='Pending'or ipnt.[Status]='Submitted') and ipnt.[Type] ='Interview' and im.IsEmergency=1)  
   
End  
  
  
  
  
End  
GO


