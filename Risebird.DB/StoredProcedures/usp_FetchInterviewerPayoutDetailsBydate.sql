IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_FetchInterviewerPayoutDetailsBydate'))
BEGIN
    DROP PROCEDURE dbo.usp_FetchInterviewerPayoutDetailsBydate
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create Procedure usp_FetchInterviewerPayoutDetailsBydate  
@st datetime,  
@se datetime  
AS  
BEGIN  
;with CTE as(Select Distinct ip.id,(ul.FirstName+ul.LastName) as FirstName,ip.Userid,ul.EmailId as InterviewerEmailid,  
(ip.Points) as Points,tic.AccountNumber,tic.PAN,tic.IFSCCode,tic.AccountName  
from InterviewPoints ip join UserList ul on ul.id=ip.userid   
left join InterviewMeetings im on im.id = ip.InterviewId   
left join InterviewerProfile irp on irp.UserId=ul.id  
left join TransInterviewerAccDetails tic on tic.InterviewerProfileId=irp.id  
where ip.Interviewid!=0  and (ip.Status='Submitted' or ip.Status='Pending')  
and (isNull(im.interviewdate,ip.Date)>=@st   
and isNull(im.interviewdate,ip.Date)<=@se)   
Union     
Select Distinct ip.id,(ul.FirstName+ul.LastName) as FirstName,ip.Userid,ul.EmailId as InterviewerEmailid,  
(ip.Points) as Points,tic.AccountNumber,tic.PAN,tic.IFSCCode,tic.AccountName  
from InterviewPoints ip join UserList ul on ul.id=ip.userid   
left join UnapprovedInterviewerMeetings im on im.id = ip.InterviewerId   
left join InterviewerProfile irp on irp.UserId=ul.id  
left join TransInterviewerAccDetails tic on tic.InterviewerProfileId=irp.id  
where ip.Interviewerid!=0  and (ip.Status='Submitted' or ip.Status='Pending') and (isNull(im.interviewdate,ip.Date)>=@st   
and isNull(im.interviewdate,ip.Date)<=@se)    
union   
Select  Distinct ip.id,(ul.FirstName+ul.LastName) as FirstName,ip.Userid,ul.EmailId as InterviewerEmailid,  
(ip.Points) as Points,tic.AccountNumber,tic.PAN,tic.IFSCCode,tic.AccountName  
from InterviewPoints ip join UserList ul on ul.id=ip.userid     
left join UnapprovedInterviewerMeetings im on im.id = ip.InterviewerId     
left join InterviewerProfile irp on irp.UserId=ul.id  
left join TransInterviewerAccDetails tic on tic.InterviewerProfileId=irp.id  
where ip.Interviewerid=0 and ip.Interviewid=0 and (ip.Status='Submitted' or ip.Status='Pending')   
and (isNull(im.interviewdate,ip.Date)>=@st and isNull(im.interviewdate,ip.Date)<=@se))  
Select distinct FirstName,Userid,InterviewerEmailid,AccountNumber,PAN,IFSCCode,AccountName,sum(Points) as Points from CTE  
Group by FirstName,Userid,InterviewerEmailid,AccountNumber,PAN,IFSCCode,AccountName  
END
GO