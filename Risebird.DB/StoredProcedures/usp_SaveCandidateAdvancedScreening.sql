IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_SaveCandidateAdvancedScreening'))
BEGIN
    DROP PROCEDURE dbo.usp_SaveCandidateAdvancedScreening
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

-- Advanced Resume Screening: upserts one candidate's result by @CandidateId and increments
-- AttemptCount. A Failed or Skipped save keeps the last completed scores, so a failed re-screen
-- does not erase the previous result from the list and report.
Create PROCEDURE [dbo].[usp_SaveCandidateAdvancedScreening]
@CandidateId int,
@IRFId int,
@Status varchar(20),
@RubricVersion int = null,
@InstructionsVersion int = null,
@TotalScore decimal(6,2) = null,
@OverallPercentage decimal(5,2) = null,
@Verdict varchar(30) = null,
@SkillsScore decimal(6,2) = null,
@WorkScore decimal(6,2) = null,
@EducationScore decimal(6,2) = null,
@AuthenticityRating varchar(20) = null,
@AuthenticityFlagCount int = null,
@ResponseJson nvarchar(max) = null,
@FactsJson nvarchar(max) = null,
@ErrorMessage nvarchar(1000) = null,
@TriggeredBy varchar(200) = null
AS
Begin
	SET NOCOUNT ON;

	declare @completed bit = case when @Status = 'Completed' then 1 else 0 end

	if exists (select 1 from CandidateAdvancedScreening where CandidateId = @CandidateId)
	Begin
		update CandidateAdvancedScreening set
			IRFId = @IRFId,
			[Status] = @Status,
			RubricVersion = case when @completed = 1 then @RubricVersion else RubricVersion end,
			InstructionsVersion = case when @completed = 1 then @InstructionsVersion else InstructionsVersion end,
			TotalScore = case when @completed = 1 then @TotalScore else TotalScore end,
			OverallPercentage = case when @completed = 1 then @OverallPercentage else OverallPercentage end,
			Verdict = case when @completed = 1 then @Verdict else Verdict end,
			SkillsScore = case when @completed = 1 then @SkillsScore else SkillsScore end,
			WorkScore = case when @completed = 1 then @WorkScore else WorkScore end,
			EducationScore = case when @completed = 1 then @EducationScore else EducationScore end,
			AuthenticityRating = case when @completed = 1 then @AuthenticityRating else AuthenticityRating end,
			AuthenticityFlagCount = case when @completed = 1 then @AuthenticityFlagCount else AuthenticityFlagCount end,
			ResponseJson = case when @completed = 1 then @ResponseJson else ResponseJson end,
			FactsJson = ISNULL(@FactsJson, FactsJson),
			ErrorMessage = case when @completed = 1 then null else @ErrorMessage end,
			AttemptCount = AttemptCount + 1,
			ScreenedOn = GETUTCDATE(),
			TriggeredBy = ISNULL(@TriggeredBy, TriggeredBy),
			ClaimedOn = null
		where CandidateId = @CandidateId
	End
	else
	Begin
		insert into CandidateAdvancedScreening
		(CandidateId, IRFId, [Status], RubricVersion, InstructionsVersion, TotalScore, OverallPercentage, Verdict,
		 SkillsScore, WorkScore, EducationScore, AuthenticityRating, AuthenticityFlagCount, ResponseJson, FactsJson,
		 ErrorMessage, AttemptCount, ScreenedOn, TriggeredBy, QueuedOn)
		values
		(@CandidateId, @IRFId, @Status, @RubricVersion, @InstructionsVersion, @TotalScore, @OverallPercentage, @Verdict,
		 @SkillsScore, @WorkScore, @EducationScore, @AuthenticityRating, @AuthenticityFlagCount, @ResponseJson, @FactsJson,
		 case when @completed = 1 then null else @ErrorMessage end, 1, GETUTCDATE(), @TriggeredBy, GETUTCDATE())
	End
End
GO
