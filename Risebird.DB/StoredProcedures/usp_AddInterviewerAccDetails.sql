/****** Object:  StoredProcedure [dbo].[usp_AddInterviewerAccDetails]    Script Date: 5/1/2020 4:02:17 PM ******/

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_AddInterviewerAccDetails'))
BEGIN
    DROP PROCEDURE [dbo].[usp_AddInterviewerAccDetails]
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO




Create PROCEDURE [dbo].[usp_AddInterviewerAccDetails]

@InterviewerProfileId int,
@AccountName varchar(200),
@AccountNumber nvarchar(200),
@IFSCCode nvarchar(200),
@UPIId nvarchar(250),
@PAN nvarchar(200)

AS
Begin
INSERT INTO [dbo].[TransInterviewerAccDetails]
           ([InterviewerProfileId]
           ,[AccountName]
           ,[AccountNumber]
           ,[IFSCCode]
           ,[UPIId]
           ,[PAN])
     VALUES
           (@InterviewerProfileId,
			@AccountName,
			@AccountNumber,
			@IFSCCode,
			@UPIId,
			@PAN)

End
GO

