IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_SaveInterviewerPreOnboardingVerdictV2'))
BEGIN
    DROP PROCEDURE dbo.usp_SaveInterviewerPreOnboardingVerdictV2
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create PROCEDURE [dbo].[usp_SaveInterviewerPreOnboardingVerdictV2]     
@EmailId [varchar](50),
@PrimarySpecialization [varchar](250),
--@screeningResponseJson varchar(max),
@completeFitmentAnalysis varchar(max) = null,
@fitmentScore int = null,
@verdict varchar(25) = null,
@PerformedBy varchar(200),
@timeNow datetime,
@result bit out
As
Begin

select @result = 0

if exists(select top 1 id from PreOnboardingInterviewerAIVerdict where EmailId = @EmailId)
Begin
	if exists(select top 1 id from PreOnboardingInterviewerAIVerdict where EmailId = @EmailId and PrimarySpecialization = @PrimarySpecialization)
	Begin
		update PreOnboardingInterviewerAIVerdict set OpenAIResponse = @completeFitmentAnalysis, AIVerdict =@verdict,
		ModifiedBy =@PerformedBy, ModifiedAt =@timeNow, AIScreeningScore = @fitmentScore 
		where EmailId = @EmailId and PrimarySpecialization = @PrimarySpecialization

		select @result = 1
	End	
End
else
Begin
	insert into PreOnboardingInterviewerAIVerdict(EmailId, PrimarySpecialization, OpenAIResponse, AIVerdict, CreatedAt, CreatedBy, AIScreeningScore)
	values (@EmailId, @PrimarySpecialization, @completeFitmentAnalysis, @verdict, @timeNow, @PerformedBy, @fitmentScore)

	select @result = 1
End
 
End
GO