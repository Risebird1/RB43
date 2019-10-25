/****** Object:  StoredProcedure [dbo].[usp_CheckCompletedInterviews]    Script Date: 25-09-2019 7.40.51 PM ******/

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_CheckCompletedInterviews'))
BEGIN
    DROP PROCEDURE usp_CheckCompletedInterviews
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

create PROCEDURE [dbo].[usp_CheckCompletedInterviews]     
@emailID varchar(200),
@ClientId int,
@irfId int
AS      
BEGIN   

select a.id from  (select InterviewMeetings.*, irf.id as irfIntId,irf.ClientId , irf.JDLink,irf.JobLevel, ic.ApprovalComments, ic.IsApproved,ic.InterviewStatus, ic.MeetingStatus,ic.IsSelected,ic.VideoLink from InterviewMeetings    
 inner join IRF  irf on InterviewMeetings.IRFId=irf.IRFId left join InterviewComments ic on InterviewMeetings.id = ic.InterviewId     
 where confirmationStatus = 1) as a where a.ClientId=@ClientId and a.candidateEmail=@emailID and a.irfIntId =@irfId  and a.MeetingStatus=4   
      
END   
GO




