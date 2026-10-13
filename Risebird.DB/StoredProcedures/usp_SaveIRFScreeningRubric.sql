IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_SaveIRFScreeningRubric'))
BEGIN
    DROP PROCEDURE dbo.usp_SaveIRFScreeningRubric
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

-- Advanced Resume Screening: upserts an IRF's screening rubric row. Every part is optional so
-- one procedure serves each step - a NULL parameter leaves its column unchanged:
--
--   @SaveInstructions = 1   writes @Instructions and the two Apply flags. InstructionsVersion
--                           goes up by one when any of the three actually changes.
--   @JDText                 the JD text extracted at generation (or corrected by the recruiter).
--   @RubricJson             a generated or edited rubric, with @TotalWeight.
--   @Status                 Draft, Generating, Generated, Approved or Failed (with @LastError).
--   @Approve = 1            marks the rubric Approved. RubricVersion goes up by one when the
--                           approved content is new: first approval, or the JSON changed.
--
-- Saving an edited rubric without approving it takes an Approved rubric back to the passed
-- status, so the job stops screening against content nobody has approved.
Create PROCEDURE [dbo].[usp_SaveIRFScreeningRubric]
@IRFId int,
@User varchar(200),
@SaveInstructions bit = 0,
@Instructions nvarchar(2000) = null,
@ApplyInstructionsToRubric bit = null,
@ApplyInstructionsToScoring bit = null,
@JDText nvarchar(max) = null,
@RubricJson nvarchar(max) = null,
@TotalWeight int = null,
@Status varchar(20) = null,
@LastError nvarchar(1000) = null,
@Approve bit = 0
AS
Begin
	SET NOCOUNT ON;

	if not exists (select 1 from IRFScreeningRubric where IRFId = @IRFId)
	Begin
		insert into IRFScreeningRubric (IRFId, [Status], CreatedBy, CreatedOn)
		values (@IRFId, 'Draft', @User, GETUTCDATE())
	End

	if (@SaveInstructions = 1)
	Begin
		update IRFScreeningRubric set
			InstructionsVersion = InstructionsVersion +
				case when ISNULL(Instructions, N'') <> ISNULL(@Instructions, N'')
				       or ApplyInstructionsToRubric <> ISNULL(@ApplyInstructionsToRubric, ApplyInstructionsToRubric)
				       or ApplyInstructionsToScoring <> ISNULL(@ApplyInstructionsToScoring, ApplyInstructionsToScoring)
				     then 1 else 0 end,
			Instructions = NULLIF(LTRIM(RTRIM(@Instructions)), N''),
			ApplyInstructionsToRubric = ISNULL(@ApplyInstructionsToRubric, ApplyInstructionsToRubric),
			ApplyInstructionsToScoring = ISNULL(@ApplyInstructionsToScoring, ApplyInstructionsToScoring),
			InstructionsUpdatedBy = @User,
			InstructionsUpdatedOn = GETUTCDATE(),
			ModifiedBy = @User,
			ModifiedOn = GETUTCDATE()
		where IRFId = @IRFId
	End

	if (@JDText is not null)
	Begin
		update IRFScreeningRubric set JDText = @JDText, ModifiedBy = @User, ModifiedOn = GETUTCDATE()
		where IRFId = @IRFId
	End

	if (@RubricJson is not null or @Status is not null or @Approve = 1)
	Begin
		update IRFScreeningRubric set
			RubricVersion = RubricVersion +
				case when @Approve = 1
				      and ([Status] <> 'Approved' or ISNULL(RubricJson, N'') <> ISNULL(@RubricJson, ISNULL(RubricJson, N'')))
				     then 1 else 0 end,
			RubricJson = ISNULL(@RubricJson, RubricJson),
			TotalWeight = ISNULL(@TotalWeight, TotalWeight),
			[Status] = case when @Approve = 1 then 'Approved' else ISNULL(@Status, [Status]) end,
			LastError = case when @Approve = 1 or @Status in ('Generated', 'Approved', 'Draft') then null
			                 else ISNULL(@LastError, LastError) end,
			GeneratedOn = case when @Status = 'Generated' then GETUTCDATE() else GeneratedOn end,
			ApprovedBy = case when @Approve = 1 then @User else ApprovedBy end,
			ApprovedOn = case when @Approve = 1 then GETUTCDATE() else ApprovedOn end,
			ModifiedBy = @User,
			ModifiedOn = GETUTCDATE()
		where IRFId = @IRFId
	End

	select RubricVersion, InstructionsVersion, [Status]
	from IRFScreeningRubric where IRFId = @IRFId
End
GO
