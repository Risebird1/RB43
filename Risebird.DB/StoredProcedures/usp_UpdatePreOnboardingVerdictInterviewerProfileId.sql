IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_UpdatePreOnboardingVerdictInterviewerProfileId'))
BEGIN
    DROP PROCEDURE dbo.usp_UpdatePreOnboardingVerdictInterviewerProfileId
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_UpdatePreOnboardingVerdictInterviewerProfileId]
    @id                 int,
    @InterviewerProfileId int,
    @ModifiedBy         varchar(100),
    @timeNow            datetime
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE PreOnboardingInterviewerAIVerdict
    SET    InterviewerProfileId = @InterviewerProfileId,
           ModifiedAt           = @timeNow,
           ModifiedBy           = @ModifiedBy
    WHERE  id                   = @id
      AND  InterviewerProfileId IS NULL
END
GO
