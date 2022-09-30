IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_NoshowInterviewerinactive'))
BEGIN
    DROP PROCEDURE dbo.usp_NoshowInterviewerinactive
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

Create Procedure usp_NoshowInterviewerinactive  
@interviewer varchar(150)  
as  
Begin  
update userlist set isactive=0 where emailid in (  
select im.interviewer from InterviewMeetings im  
inner join interviewcomments ic on ic.interviewid=im.id  
where (ic.meetingstatus=5 or ic.meetingstatus=9 ) and im.confirmationstatus=1 and im.interviewdate>=dateadd(day,-30,dateadd (minute,330,getdate())) and im.interviewdate<dateadd (minute,330,getdate())  
and im.interviewer=@interviewer  
group by im.interviewer   
HAVING  
count (im.id)> 1) and idprofile=3 

update TransInterviewer set IsActive=0 where UserId in (
select ul.id from InterviewMeetings im  
inner join interviewcomments ic on ic.interviewid=im.id  
inner join  UserList ul on ul.EmailId=im.interviewer
where (ic.meetingstatus=5 or ic.meetingstatus=9 ) and im.confirmationstatus=1 and im.interviewdate>=dateadd(day,-30,dateadd (minute,330,getdate())) and im.interviewdate<dateadd (minute,330,getdate())
and im.interviewer=@interviewer 
group by ul.id 
HAVING  
count (im.id)> 1 )
END    
GO 
