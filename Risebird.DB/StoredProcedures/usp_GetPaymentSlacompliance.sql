IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetPaymentSlacompliance'))
BEGIN
    DROP PROCEDURE dbo.usp_GetPaymentSlacompliance
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE usp_GetPaymentSlacompliance    
@actionstatus varchar(20)    
AS    
BEGIN    
if(@actionstatus='SLACompleted')    
begin    
Select ps.*,IRF.IRFId,IRF.id as irfIntId,im.candidateEmail,im.interviewer,im.interviewDate,ul.EmailId as ChannelOwner,ip.CompanyName, ip.ResumeLink, ip.RedMarker, uli.MobileNumber,RTRIM(LTRIM((ISNULL(uli.FirstName,'')+' '+ISNULL(uli.LastName,'')))) as InterviewerName,tq.Redflags,Case when tq.id>0 then 1 else 1 end as QCStatus,ps.IsApproved,    
imj.Joinedon    
From PaymentSLADetails ps left join InterviewMeetings im on ps.Interviewid=im.id     
left join IRF on IRF.IRFId=im.IRFId     
left join UserList ul on ul.id=IRF.RisebirdHr    
left join UserList uli on uli.EmailId=im.interviewer    
left join InterviewerProfile ip on ip.UserId=uli.id    
left join Trans_QCFormReport tq on tq.interviewid=im.id    
outer apply(Select Min(Joinedon) Joinedon from InterviewMeetingsJoinTime where interviewid=im.id) as imj
where ISNull(ps.IsApproved,0)!=0    
end    
else    
begin    
Select ps.*,IRF.IRFId,IRF.id as irfIntId,im.candidateEmail,im.interviewer,im.interviewDate,ul.EmailId as ChannelOwner,ip.CompanyName, ip.ResumeLink, ip.RedMarker, uli.MobileNumber,RTRIM(LTRIM((ISNULL(uli.FirstName,'')+' '+ISNULL(uli.LastName,'')))) as InterviewerName,tq.Redflags,Case when tq.id>0 then 1 else 1 end as QCStatus,    
imj.Joinedon    
From PaymentSLADetails ps left join InterviewMeetings im on ps.Interviewid=im.id     
left join IRF on IRF.IRFId=im.IRFId     
left join UserList ul on ul.id=IRF.RisebirdHr    
left join UserList uli on uli.EmailId=im.interviewer    
left join InterviewerProfile ip on ip.UserId=uli.id    
left join Trans_QCFormReport tq on tq.interviewid=im.id 
outer apply(Select Min(Joinedon) Joinedon from InterviewMeetingsJoinTime where interviewid=im.id) as imj
where ISNull(ps.IsApproved,0)=0    
end    
END 
GO