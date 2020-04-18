/****** Object:  StoredProcedure [dbo].[usp_GetIRFInterviewMeetings]    Script Date: 15-10-2019 1.50.19 PM ******/
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetIRFInterviewMeetings'))
BEGIN
    DROP PROCEDURE dbo.usp_GetIRFInterviewMeetings
END
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

Create PROCEDURE [dbo].[usp_GetIRFInterviewMeetings]

@id int

AS
Begin

declare
@IRFId nvarchar(100) = (select IRFId from [dbo].[IRF] where id=@id)

select irf.PerMintuteRate, irf.TotalBudget,QCF.InterviewId as QCInterview,im.id imid, * from InterviewMeetings im
left join [dbo].[InterviewComments] ic on im.id = ic.InterviewId left join irf on IRF.IRFId = im.IRFId
left join QCFeedbackReport QCF on im.id=QCF.InterviewId
 where im.IRFId = @IRFId


End

GO


