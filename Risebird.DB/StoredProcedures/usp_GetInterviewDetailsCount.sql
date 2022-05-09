IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetInterviewDetailsCount'))
BEGIN
    DROP PROCEDURE [dbo].[usp_GetInterviewDetailsCount]
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

  
Create PROCEDURE [dbo].[usp_GetInterviewDetailsCount]             
@emailID varchar(200),
@timeNow datetime  
AS              
BEGIN        
      
select Count(im.id) as MyInterviewCount from InterviewMeetings im               
inner join IRF  irf on im.IRFId=irf.IRFId left join InterviewComments ic on im.id = ic.InterviewId                
left join UserList ul on ul.EmailId = im.candidateEmail            
join  CandidatesMaster cm on ul.id=cm.UserId            
join Candidate ca on irf.id=ca.IRFId and cm.id = ca.CandidateMasterId            
left join UserList ulh on ulh.EmailId = im.HRCoordinatorEmailId            
where interviewer =@emailID and confirmationStatus = 1 and (im.interviewDate>=@timeNow or (ic.MeetingStatus is null and im.interviewDate<@timeNow))                   
   
;With CTE as(
select im.id from InterviewMeetings im        
inner join IRF irf on im.IRFId=irf.IRFId         
inner join [dbo].[TransInterviewer] tirf on IRF.id =tirf.IRFId        
left join [dbo].[Interviewer] inte on tirf.InterviewerId = inte.id    
left join [dbo].[UserList] ul on inte.UserId = ul.id     
left join UserList ulc on ulc.EmailId = im.candidateEmail      
join CandidatesMaster cm on cm.UserId = ulc.id      
join Candidate ca on ca.CandidateMasterId = cm.id and ca.IRFId=irf.id      
where interviewer is null and confirmationStatus = 1 and IsEmergency=0 and ul.EmailId = @emailID and im.interviewDate>=@timeNow and tirf.IsActive=1    
union     
select im.id from InterviewMeetings im        
inner join IRF irf on im.IRFId=irf.IRFId         
inner join [dbo].[TransInterviewer] tirf on IRF.id =tirf.IRFId        
left join [dbo].[InterviewerProfile] inte on tirf.InterviewerId = inte.id    
left join [dbo].[UserList] ul on inte.UserId = ul.id     
left join UserList ulc on ulc.EmailId = im.candidateEmail      
join CandidatesMaster cm on cm.UserId = ulc.id      
join Candidate ca on ca.CandidateMasterId = cm.id and ca.IRFId=irf.id      
where interviewer is null and confirmationStatus = 1 and IsEmergency=0 and ul.EmailId = @emailID and im.interviewDate>=@timeNow and tirf.IsActive=1)
Select Count(*) OpenInterviewCount from CTE   

select Count(im.id) as PendingReportCount from InterviewMeetings im               
inner join IRF  irf on im.IRFId=irf.IRFId left join InterviewComments ic on im.id = ic.InterviewId                
left join UserList ul on ul.EmailId = im.candidateEmail            
join  CandidatesMaster cm on ul.id=cm.UserId            
join Candidate ca on irf.id=ca.IRFId and cm.id = ca.CandidateMasterId            
left join UserList ulh on ulh.EmailId = im.HRCoordinatorEmailId            
where interviewer =@emailID and confirmationStatus = 1 and (ic.MeetingStatus is null or ic.MeetingStatus=2)
END 
GO

    
      