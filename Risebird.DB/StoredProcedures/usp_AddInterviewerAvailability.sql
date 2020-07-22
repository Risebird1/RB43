/****** Object:  StoredProcedure [dbo].[usp_AddInterviewerAvailability]    Script Date: 5/1/2020 4:01:50 PM ******/

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_AddInterviewerAvailability'))
BEGIN
    DROP PROCEDURE [dbo].[usp_AddInterviewerAvailability]
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO




Create PROCEDURE [dbo].[usp_AddInterviewerAvailability]

@InterviewerProfileId int,
@Weekday nvarchar(50),
@Weekend nvarchar(50),
@IsAvailableOnPublicHolidays bit

AS
Begin
INSERT INTO [dbo].[TransInterviewerAvailability]
           ([InterviewerProfileId]
           ,[Weekday]
           ,[Weekend]
           ,[IsAvailableOnPublicHolidays])
     VALUES
           (@InterviewerProfileId,
			@Weekday,
			@Weekend,
			@IsAvailableOnPublicHolidays)

End
GO

