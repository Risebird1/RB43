IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetRevenueDetailsByClientBillingOwner'))
BEGIN
    DROP PROCEDURE dbo.usp_GetRevenueDetailsByClientBillingOwner
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE usp_GetRevenueDetailsByClientBillingOwner    
@Clientid int,        
@StartDate datetime,        
@EndDate datetime,
@year int,
@month int
AS        
BEGIN        
Select cl.id,cl.Name,Count(im.id) as TotalCount,ul.EmailId as BillingOwner,ul.id as BillingOwnerid,rd.CurrentStatus From InterviewMeetings im join IRF on IRF.IRFId=im.IRFId         
join InterviewComments ic on ic.InterviewId=im.id join Client cl on IRF.ClientId=cl.id        
join UserList ul on ul.id=IRF.BillingOwner  
left join Revenuedetails rd on rd.Clientid=IRF.ClientId and rd.Months=@month and rd.Years=@year and rd.BillingOwner=ul.id
where im.interviewDate>=@StartDate and im.interviewDate<@EndDate and cl.id=@Clientid        
and (ic.MeetingStatus=4 or ic.MeetingStatus=6 or ic.MeetingStatus=7 or ic.MeetingStatus=8)          
group by cl.id,cl.Name,ul.EmailId,ul.id,rd.CurrentStatus   
order by ul.EmailId
END        

Go
