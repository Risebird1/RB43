IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetInterviewerPreOnboardingAIResponse'))
BEGIN
    DROP PROCEDURE dbo.usp_GetInterviewerPreOnboardingAIResponse
END
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
Create PROCEDURE usp_GetInterviewerPreOnboardingAIResponse

@EmailId varchar(100),
@PrimarySpecialization varchar(250)
AS
BEGIN

select top 1 OpenAIResponse as AIResponse, AIScreeningScore, AIVerdict from PreOnboardingInterviewerAIVerdict 
where EmailId =@EmailId and PrimarySpecialization = @PrimarySpecialization 
order by CreatedAt desc

END
Go