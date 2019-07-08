IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_OpenInterviews'))
BEGIN
    DROP PROCEDURE dbo.usp_OpenInterviews
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_OpenInterviews] 
  
AS
BEGIN
select * from [dbo].[InterviewMeetings] where interviewer is null and comfirmationStatus = 'Confirmed'

END
GO 