/****** Object:  StoredProcedure [dbo].[usp_CandidateFeedbackDetails]    Script Date: 10/10/2023 11:13:12 AM ******/

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_CandidateFeedbackDetails'))
BEGIN
    DROP PROCEDURE dbo.usp_CandidateFeedbackDetails
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_CandidateFeedbackDetails]
@Rating int,
@CandidateInteractionQuality varchar(250),
@LimitedHandoncoding varchar(250),
@WeakInfrastructure varchar(250),
@UnethicalPractices varchar(250),
@TimeManagement varchar(250),
@PoorCommunicationSkills varchar(250),
@CreatedOn DATETIME,
@InterviewId int
AS
Begin
if Exists(Select * from CandidateFeedbackDetails where InterviewId=@InterviewId)
Begin
Update CandidateFeedbackDetails set Rating=@Rating,CandidateInteractionQuality=@CandidateInteractionQuality,LimitedHandoncoding=@LimitedHandoncoding, 
WeakInfrastructure=@WeakInfrastructure,UnethicalPractices=@UnethicalPractices,TimeManagement=@TimeManagement,PoorCommunicationSkills=@PoorCommunicationSkills,
CreatedOn=@CreatedOn
Where InterviewId=@InterviewId
End
Else
Begin
Insert into CandidateFeedbackDetails(Rating,CandidateInteractionQuality,LimitedHandoncoding,WeakInfrastructure,UnethicalPractices,
TimeManagement,PoorCommunicationSkills,CreatedOn,InterviewId)values(@Rating,@CandidateInteractionQuality,@LimitedHandoncoding,@WeakInfrastructure,@UnethicalPractices,
@TimeManagement,@PoorCommunicationSkills,@CreatedOn,@InterviewId)
End
End

GO


