/****** Object:  StoredProcedure [dbo].[usp_MyQualityControlInterviews]    Script Date: 13-11-2019 3.00.19 PM ******/
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_MyQualityControlInterviews'))
BEGIN
    DROP PROCEDURE dbo.usp_MyQualityControlInterviews
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

Create PROCEDURE [dbo].[usp_MyQualityControlInterviews]     
@emailID varchar(200)        
AS      
BEGIN      
select qcfr.*,InterviewMeetings.*, irf.id as irfIntId,irf.ClientId, irf.JDLink,irf.JobLevel, ic.ApprovalComments, ic.IsApproved,ic.InterviewStatus,ic.IsSelected, ic.MeetingStatus,ic.VideoLink, ic.QCStatus, ic.QCComment from InterviewMeetings    
 inner join IRF  irf on InterviewMeetings.IRFId=irf.IRFId left join InterviewComments ic on InterviewMeetings.id = ic.InterviewId left join QCFeedbackReport qcfr on qcfr.InterviewId = InterviewMeetings.id    
 where QCInterviewer = @emailID 
END   
GO


