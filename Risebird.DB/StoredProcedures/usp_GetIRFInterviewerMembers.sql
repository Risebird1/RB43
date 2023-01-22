/****** Object:  StoredProcedure [dbo].[usp_GetIRFInterviewerMembers]    Script Date: 04-09-2020 12.06.07 PM ******/
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetIRFInterviewerMembers'))
BEGIN
    DROP PROCEDURE dbo.usp_GetIRFInterviewerMembers
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

Create PROCEDURE [dbo].[usp_GetIRFInterviewerMembers]  
@IRFId int  
AS  
Begin      
declare @commonTable table (IRFId int, [Type] varchar(25), EmailId varchar(100), ResumeLink varchar(500),TotalInterviews int, TotalQCInterviews int, MobileNumber varchar(30),uniqueid varchar(50),InterviewerName varchar(100),IsActive bit,IsAvailable bit,SendIntervieweravailbilityOn datetime,AvailbilityHour int)  
 insert into @commonTable       
  select ti.IRFId, 'Interviewer' as [Type] ,ul.EmailId, ipr.ResumeLink,      
  dbo.fn_GetInterviewerOrQCMeetingsCount(IRF.IRFId, ul.EmailId, 'I') as TotalInterviews,      
  0 as TotalQCInterviews, ul.MobileNumber,ul.uniqueid,(ul.FirstName+' '+ul.LastName) as InterviewerName,ti.IsActive,ti.IsAvailable,
  IRF.SendIntervieweravailbilityOn,IsNull(DATEDIFF(SECOND, IRF.SendIntervieweravailbilityOn, GetDate()),0) as AvailbilityHour
  from TransInterviewer ti join InterviewerProfile ipr on ipr.UserId = ti.UserId      
  join UserList ul on ul.id = ti.UserId      
  join IRF on irf.id = ti.IRFId      
  where irf.id=@IRFId and ti.IsActive=1 and ul.IsActive=1     
  group by ti.IRFId, ul.EmailId, ipr.ResumeLink, ul.MobileNumber, IRF.IRFId,ul.uniqueid,ul.FirstName+' '+ul.LastName,ti.IsActive,ti.IsAvailable,IRF.SendIntervieweravailbilityOn      
      
 insert into @commonTable       
  select tqc.IRFId, 'QCInterviewer' as [Type] ,ul.EmailId, ipr.ResumeLink,      
  0 as TotalInterviews,      
  dbo.fn_GetInterviewerOrQCMeetingsCount(IRF.IRFId, ul.EmailId, 'QC') as TotalQCInterviews,      
  ul.MobileNumber,ul.uniqueid,(ul.FirstName+' '+ul.LastName) as InterviewerName,1 as IsActive,1 as IsAvailable,
  IRF.SendIntervieweravailbilityOn,IsNull(DATEDIFF(SECOND, IRF.SendIntervieweravailbilityOn, GetDate()),0) as AvailbilityHour
  from TransQualityController tqc join InterviewerProfile ipr on ipr.UserId = tqc.UserId      
  join UserList ul on ul.id = tqc.UserId      
  join IRF on irf.id = tqc.IRFId      
  where tqc.IRFId =@IRFId and tqc.IsActive=1 and ul.IsActive=1     
  group by tqc.IRFId, ul.EmailId, ipr.ResumeLink, ul.MobileNumber, IRF.IRFId,ul.uniqueid,ul.FirstName+' '+ul.LastName,IRF.SendIntervieweravailbilityOn    
      
 insert into @commonTable       
  select tei.IRFId, 'Emergency Interviewer' as [Type] ,ul.EmailId, ipr.ResumeLink,      
  dbo.fn_GetInterviewerOrQCMeetingsCount(IRF.IRFId, ul.EmailId, 'EI') as TotalInterviews,      
  0 as TotalQCInterviews,        
  ul.MobileNumber,ul.uniqueid,(ul.FirstName+' '+ul.LastName) as InterviewerName,tei.IsActive,1 as IsAvailable,
  IRF.SendIntervieweravailbilityOn,IsNull(DATEDIFF(SECOND, IRF.SendIntervieweravailbilityOn, GetDate()),0) as AvailbilityHour
  from TransEmergencyInterviewer tei join InterviewerProfile ipr on ipr.UserId = tei.UserId      
  join UserList ul on ul.id = tei.UserId      
  join IRF on irf.id = tei.IRFId      
  where  tei.IRFId = @IRFId and tei.IsActive=1 and ul.IsActive=1     
  group by tei.IRFId, ul.EmailId, ipr.ResumeLink, ul.MobileNumber, IRF.IRFId ,ul.uniqueid,ul.FirstName+' '+ul.LastName,tei.IsActive,IRF.SendIntervieweravailbilityOn    
      
select * from @commonTable         
      
End 
GO


