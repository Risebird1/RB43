/****** Object:  StoredProcedure [dbo].[usp_MyInterviews]    Script Date: 25-09-2019 7.40.51 PM ******/
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_MyInterviews'))
BEGIN
    DROP PROCEDURE dbo.usp_MyInterviews
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

 
CREATE PROCEDURE [dbo].[usp_MyInterviews]     
@emailID varchar(200)        
AS      
BEGIN      
select InterviewMeetings.*, irf.id as irfIntId,irf.ClientId, irf.JDLink,irf.JobLevel,irf.CompanyDetails, ic.ApprovalComments, ic.IsApproved,ic.InterviewStatus,ic.IsSelected, ic.MeetingStatus,ic.VideoLink, cm.id as CandidateMasterId from InterviewMeetings    
 inner join IRF  irf on InterviewMeetings.IRFId=irf.IRFId left join InterviewComments ic on InterviewMeetings.id = ic.InterviewId    
 left join  CandidatesMaster cm on InterviewMeetings.candidateEmail=cm.EmailId
 where interviewer =@emailID and confirmationStatus = 1        
      
END   
GO


