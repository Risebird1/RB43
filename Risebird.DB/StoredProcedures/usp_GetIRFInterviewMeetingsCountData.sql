/****** Object:  StoredProcedure [dbo].[usp_GetIRFInterviewMeetingsCountData]    Script Date: 21-02-2020 1.45.27 PM ******/
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetIRFInterviewMeetingsCountData'))
BEGIN
    DROP PROCEDURE dbo.usp_GetIRFInterviewMeetingsCountData
END
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

Create PROCEDURE [dbo].[usp_GetIRFInterviewMeetingsCountData]

@id int

AS
Begin

declare
@IRFId nvarchar(100) = (select IRFId from [dbo].[IRF] where id=@id)

select im.id imid, ic.MeetingStatus, ic.InterviewStatus, im.ClientHrContact, im.staffingAgency, im.interviewDate, im.confirmationStatus,im.interviewSlot, im.interviewer from InterviewMeetings im
left join [dbo].[InterviewComments] ic on im.id = ic.InterviewId left join irf on IRF.IRFId = im.IRFId

 where im.IRFId = @IRFId

End

GO


