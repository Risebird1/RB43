/****** Object:  StoredProcedure [dbo].[usp_GetClientMeetings]    Script Date: 25-09-2019 7.40.51 PM ******/
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_GetClientMeetings'))
BEGIN
    DROP PROCEDURE dbo.usp_GetClientMeetings
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

 
CREATE PROCEDURE [dbo].[usp_GetClientMeetings]     
@EmailId varchar(300)        
AS      
BEGIN      
select im.*, im.id as imid, IRF.ClientId,IRF.ClientName,IRF.JDTiltle, IRF.IsBiometricEnabled, IRF.id as irfIntId,ic.MeetingStatus,ic.InterviewStatus,ic.VideoLink, ic.InterviewerComment,ic.OfferStatus,ic.JoiningStatus,ic.IsApproved,ic.IsSelected, ic.OfferComments,ic.JoiningComments,ic.ApprovalComments,ic.FinalComments,QCF.InterviewId as QCInterview, cm.id as CandidateId,(select distinct Isonline from UserList where Emailid=im.candidateEmail) as IsOnline from InterviewMeetings im 
inner join IRF on im.IRFId = IRF.IRFId 
left join [dbo].[InterviewComments] ic on im.id = ic.InterviewId 
left join QCFeedbackReport QCF on im.id=QCF.InterviewId 
left join CandidatesMaster cm on im.candidateEmail = cm.EmailId
inner join TransClientUser tcu on IRF.id=tcu.IRFId
inner join UserList ul on tcu.ClientUserId= ul.id
where ul.EmailId LIKE @EmailId AND ul.IsActive=1      
      
END   
GO


