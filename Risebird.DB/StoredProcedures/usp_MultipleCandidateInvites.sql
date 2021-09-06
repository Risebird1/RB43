IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_MultipleCandidateInvites'))
BEGIN
    DROP PROCEDURE dbo.usp_MultipleCandidateInvites
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO          
          
          
CREATE PROCEDURE [dbo].[usp_MultipleCandidateInvites]          
   @emailid varchar(150)     
          
AS          
Begin          
Declare @Role varchar(150)
Select @Role=pr.Name From UserList ul join Profile pr on pr.id=ul.idprofile where Emailid=@emailid
if(@Role='SeniorHR')
BEGIN
SELECT b.InviteCount,a.id imid,a.*,IRF.JDLink as JDLink,IRF.JDTiltle, ic.InterviewStatus,ic.MeetingStatus    
FROM [dbo].[InterviewMeetings] as a    
inner join (SELECT candidateEmail, COUNT(*) as InviteCount    
FROM [dbo].[InterviewMeetings] where interviewDate >= DATEADD(DAY,-15, GETDATE())    
GROUP BY candidateEmail    
HAVING count(*) > 1 ) as b    
ON a.candidateEmail = b.candidateEmail    
inner join IRF on a.IRFId = IRF.IRFId 
join SeniorHr sh on sh.Clientid=irf.ClientId
left join UserList ulsh on ulsh.id=sh.Userid
left join [dbo].[InterviewComments] ic on a.id = ic.InterviewId  
where ulsh.Emailid= @emailid
ORDER BY a.candidateEmail    
END
Else
BEGIN
SELECT b.InviteCount,a.id imid,a.*,IRF.JDLink as JDLink,IRF.JDTiltle, ic.InterviewStatus,ic.MeetingStatus    
FROM [dbo].[InterviewMeetings] as a    
inner join (SELECT candidateEmail, COUNT(*) as InviteCount    
FROM [dbo].[InterviewMeetings] where interviewDate >= DATEADD(DAY,-15, GETDATE())    
GROUP BY candidateEmail    
HAVING count(*) > 1 ) as b    
ON a.candidateEmail = b.candidateEmail    
inner join IRF on a.IRFId = IRF.IRFId        
left join [dbo].[InterviewComments] ic on a.id = ic.InterviewId     
ORDER BY a.candidateEmail       
END          
          
End 
GO