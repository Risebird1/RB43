/****** Object:  StoredProcedure [dbo].[usp_UpdateSettings]    Script Date: 05-03-2020 8.28.21 PM ******/
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_UpdateSettings'))
BEGIN
    DROP PROCEDURE dbo.usp_UpdateSettings
END


SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_UpdateSettings] 
@InterviewerUnassignedMinutes int,    
@AutoCancellationMailMinutes int,    
@ClientReportingMailMinutes int, 
@InterviewWindupReminderMinutes int, 
@InterviewReportReminderMinutes int, 
@QCCommentReminderMinutes int,
@QCFormdeductionRate int

AS
BEGIN
update [dbo].[Settings] 
set
[InterviewerUnassignedMinutes]=@InterviewerUnassignedMinutes,
[AutoCancellationMailMinutes]=@AutoCancellationMailMinutes,
[ClientReportingMailMinutes]=@ClientReportingMailMinutes,
[InterviewWindupReminderMinutes]=@InterviewWindupReminderMinutes,
[InterviewReportReminderMinutes]=@InterviewReportReminderMinutes,
[QCCommentReminderMinutes]=@QCCommentReminderMinutes,
[QCFormdeductionRate]=@QCFormdeductionRate

END
GO


