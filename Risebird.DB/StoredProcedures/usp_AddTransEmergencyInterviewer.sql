/****** Object:  StoredProcedure [dbo].[usp_AddTransEmergencyInterviewer]    Script Date: 12-11-2019 5.52.52 PM ******/
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_AddTransEmergencyInterviewer'))
BEGIN
    DROP PROCEDURE dbo.usp_AddTransEmergencyInterviewer
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO


CREATE PROCEDURE [dbo].[usp_AddTransEmergencyInterviewer]

@irfIntId int,
@InterviewerUserId int

AS
Begin


insert into [dbo].[TransEmergencyInterviewer] values (@irfIntId,(select id from [dbo].[Interviewer] where userid=@InterviewerUserId), @InterviewerUserId )

End


GO


