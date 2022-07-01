IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetRevenueDetailsByClient'))
BEGIN
    DROP PROCEDURE dbo.usp_GetRevenueDetailsByClient
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
  
CREATE PROCEDURE usp_GetRevenueDetailsByClient  
@Clientid int,  
@StartDate datetime,  
@EndDate datetime  
AS  
BEGIN  
Select cl.id,cl.Name,Count(im.id) as TotalCount,IRF.IRFId From InterviewMeetings im join IRF on IRF.IRFId=im.IRFId   
join InterviewComments ic on ic.InterviewId=im.id join Client cl on IRF.ClientId=cl.id  
where im.interviewDate>=@StartDate and im.interviewDate<@EndDate and cl.id=@Clientid  
and (ic.MeetingStatus=4 or ic.MeetingStatus=6 or ic.MeetingStatus=7 or ic.MeetingStatus=8)    
group by cl.id,cl.Name,IRF.IRFId  
END
GO