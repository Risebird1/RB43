
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_AddUpdateInterviewReport'))
BEGIN
    DROP PROCEDURE dbo.usp_AddUpdateInterviewReport
END
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO



CREATE PROCEDURE [dbo].[usp_AddUpdateInterviewReport]

@InterviewId int,
@IRFId int,
@CandidateName nvarchar(100),
@confirmationStatus nvarchar(30),
@OverallScore float,
@TechSkillComments nvarchar(300),
@SkillGeneralComments nvarchar(300),
@SelectabilityComments nvarchar(300),
@JoinabilityComments nvarchar(300),
@ProctoringCount int,
@PerformedBy varchar(100),
@PerformedAt datetime

AS
Begin

IF EXISTS(Select 1 from InterviewReport where InterviewId=@InterviewId)
BEGIN
	update InterviewReport 
	set
	[confirmationStatus]=@confirmationStatus
	,[OverallScore]=@OverallScore
	,[TechSkillComments]=@TechSkillComments
	,[SkillGeneralComments]=@SkillGeneralComments
	,[SelectabilityComments]=@SelectabilityComments
	,[JoinabilityComments]=@JoinabilityComments
	,[ProctoringCount]=   @ProctoringCount    
	,[ModifiedBy] = @PerformedBy
	,[ModifiedOn] = @PerformedAt
	Where InterviewId =@InterviewId


	delete from QuestionsDiscussed where InterviewId=@InterviewId
	delete from TechSkillReport where InterviewId=@InterviewId
	delete from SkillGeneralReport where InterviewId=@InterviewId
	delete from ProctoringReport where InterviewId=@InterviewId
END

ELSE
BEGIN

 Insert into InterviewReport 

 (IRFId, InterviewId, CandidateName, confirmationStatus, OverallScore, TechSkillComments, SkillGeneralComments, 
  SelectabilityComments, JoinabilityComments, ProctoringCount, CreatedBy, CreatedOn)

  values

  (@IRFId, @InterviewId, @CandidateName, @confirmationStatus, @OverallScore, @TechSkillComments, @SkillGeneralComments,
  @SelectabilityComments, @JoinabilityComments, @ProctoringCount, @PerformedBy, @PerformedAt)

END



End


GO

