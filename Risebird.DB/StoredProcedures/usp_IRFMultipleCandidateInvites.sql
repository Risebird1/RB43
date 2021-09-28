IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_IRFMultipleCandidateInvites'))
BEGIN
    DROP PROCEDURE dbo.usp_IRFMultipleCandidateInvites
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

Create PROCEDURE [dbo].[usp_IRFMultipleCandidateInvites]       
  @IRFId varchar(50),
  @emailId varchar(100) = null
        
AS        
Begin        
declare @roleId int

if(@emailId is not null)
begin
	declare @profile varchar(150)
	set @profile = (select pr.[Name] from UserList ul join [Profile] pr on pr.id = ul.idProfile where ul.EmailId=@emailId)
	if(@profile =  'Client')
	begin
		set @roleId = (select cu.RoleId from UserList ul join ClientUser cu on cu.UserId = ul.id where ul.EmailId = @emailId)
	end
end

if(@roleId=1) -- client HR for candidate
begin
	SELECT b.InviteCount,a.id imid,a.*,IRF.JDLink as JDLink,IRF.JDTiltle, ic.InterviewStatus,ic.MeetingStatus  
	FROM [dbo].[InterviewMeetings] as a  
	inner join (SELECT candidateEmail, COUNT(*) as InviteCount  
	FROM [dbo].[InterviewMeetings] imb
	join IRF on IRF.IRFId = imb.IRFId
	join UserList ul on ul.EmailId = imb.candidateEmail
	join CandidatesMaster cm on cm.UserId = ul.id
	join Candidate ca on (ca.CandidateMasterId = cm.id and ca.IRFId = irf.id)
	where imb.IRFId=@IRFId and ca.ClientHRAttemptContact = @emailId
	GROUP BY candidateEmail  
	HAVING count(*) > 1 ) as b  
	ON a.candidateEmail = b.candidateEmail  
	inner join IRF on a.IRFId = IRF.IRFId      
	left join [dbo].[InterviewComments] ic on a.id = ic.InterviewId   
	ORDER BY a.candidateEmail    

end
else
begin
	SELECT b.InviteCount,a.id imid,a.*,IRF.JDLink as JDLink,IRF.JDTiltle, ic.InterviewStatus,ic.MeetingStatus  
	FROM [dbo].[InterviewMeetings] as a  
	inner join (SELECT candidateEmail, COUNT(*) as InviteCount  
	FROM [dbo].[InterviewMeetings] where IRFId=@IRFId
	GROUP BY candidateEmail  
	HAVING count(*) > 1 ) as b  
	ON a.candidateEmail = b.candidateEmail  
	inner join IRF on a.IRFId = IRF.IRFId      
	left join [dbo].[InterviewComments] ic on a.id = ic.InterviewId   
	ORDER BY a.candidateEmail    

end
  
END
GO 