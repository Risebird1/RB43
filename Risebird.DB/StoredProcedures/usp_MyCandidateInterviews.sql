/****** Object:  StoredProcedure [dbo].[usp_MyCandidateInterviews]    Script Date: 27-11-2019 7.28.59 PM ******/

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_MyCandidateInterviews'))
BEGIN
    DROP PROCEDURE dbo.usp_MyCandidateInterviews
END
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

 
CREATE PROCEDURE [dbo].[usp_MyCandidateInterviews]     
@emailID varchar(200)        
AS      
BEGIN      
select InterviewMeetings.*, irf.id as irfIntId,irf.ClientId, irf.JDLink,irf.JobLevel,irf.CalendlyLink, irf.EndDate, irf.JDTiltle ,ic.ApprovalComments, ic.IsApproved,ic.InterviewStatus,ic.IsSelected, ic.MeetingStatus,ic.VideoLink, ul.MobileNumber as InterviewerPhone, irf.ClientName, Client.[Name] as ClientCompName from InterviewMeetings    
 inner join IRF  irf on InterviewMeetings.IRFId=irf.IRFId left join InterviewComments ic on InterviewMeetings.id = ic.InterviewId  join UserList ul on InterviewMeetings.interviewer = ul.EmailId join Client on Client.id=irf.ClientId  
 where interviewMeetings.candidateEmail =@emailID and confirmationStatus = 1         
      
END   
GO


