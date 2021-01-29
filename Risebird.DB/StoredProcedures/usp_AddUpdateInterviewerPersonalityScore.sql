
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_AddUpdateInterviewerPersonalityScore'))
BEGIN
    DROP PROCEDURE dbo.usp_AddUpdateInterviewerPersonalityScore
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

Create PROCEDURE [dbo].[usp_AddUpdateInterviewerPersonalityScore]  
  
@InterviewId int,  
@OverallPersonalityScore int,  
@Openness int,  
@Conscientiousness int,  
@Extroversion int,  
@Agreeableness int,  
@Neuroticism int
AS  
Begin  

IF NOT EXISTS (SELECT 1 FROM InterviewerPersonalityScore WHERE InterviewId= @InterviewId)
Begin
	
	Insert into InterviewerPersonalityScore (InterviewId, OverallPersonalityScore, Openness, Conscientiousness, Extroversion, Agreeableness, Neuroticism)
	values (@InterviewId, @OverallPersonalityScore,@Openness, @Conscientiousness, @Extroversion, @Agreeableness, @Neuroticism)
End

Else

Begin
	Update InterviewerPersonalityScore set
	OverallPersonalityScore=@OverallPersonalityScore,
	Openness=@Openness,
	Conscientiousness=@Conscientiousness,
	Extroversion=@Extroversion,
	Agreeableness=@Agreeableness,
	Neuroticism=@Neuroticism
	where InterviewId = @InterviewId
End

  
End  
  
  
GO
