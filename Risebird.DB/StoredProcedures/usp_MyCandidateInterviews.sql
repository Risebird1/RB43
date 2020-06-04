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
select InterviewMeetings.*, irf.id as irfIntId,irf.ClientId, irf.JDLink,irf.JobLevel,irf.CalendlyLink, irf.EndDate, irf.JDTiltle, ul.MobileNumber as InterviewerPhone, irf.ClientName, Client.[Name] as ClientCompName,
(Select Isonline from UserList where EmailId=InterviewMeetings.interviewer) as IsOnilne
from InterviewMeetings      
 inner join IRF  irf on InterviewMeetings.IRFId=irf.IRFId   
 left join UserList ul on InterviewMeetings.interviewer = ul.EmailId join Client on Client.id=irf.ClientId    
 where interviewMeetings.candidateEmail =@emailID and confirmationStatus = 1    
      
END   
GO


