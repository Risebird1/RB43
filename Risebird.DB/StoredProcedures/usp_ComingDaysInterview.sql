/****** Object:  StoredProcedure [dbo].[usp_GetUserId]    Script Date: 08-01-2020 7.40.41 PM ******/

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_ComingDaysInterview'))
BEGIN
    DROP PROCEDURE dbo.usp_ComingDaysInterview
END
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_ComingDaysInterview]
AS
Begin
Declare @endDate as date
Declare @startDate as date
SELECT @endDate= dateadd(dd,6,getdate())
SELECT @startDate= dateadd(dd,-1,getdate())
select im.IRFID, im.interviewDate, im.interviewSlot,im.interviewer,ic.interviewStatus,ic.InterviewerComment,ic.VideoLink, QCF.InterviewId as QCInterview from InterviewMeetings im
left join InterviewComments ic on im.id=ic.InterviewId
left join QCFeedbackReport QCF on im.id=QCF.InterviewId
where im.interviewDate>= @startDate and im.interviewDate<=@endDate and im.confirmationStatus=1


END

GO


