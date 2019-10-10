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
select InterviewMeetings.*, irf.id as irfIntId, irf.JDLink,irf.JobLevel, ic.ApprovalComments,ic.VideoLink from InterviewMeetings    
 inner join IRF  irf on InterviewMeetings.IRFId=irf.IRFId left join InterviewComments ic on InterviewMeetings.id = ic.InterviewId     
 where interviewer =@emailID and confirmationStatus = 1      
      
END   
GO


