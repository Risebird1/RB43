IF EXISTS (SELECT 1 FROM sys.triggers 
           WHERE Name = 'tr_PaymentSLADetails')
BEGIN
    Drop trigger tr_PaymentSLADetails
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create TRIGGER tr_PaymentSLADetails
   ON  [dbo].[PaymentSLADetails]
   AFTER  INSERT,UPDATE
AS 
BEGIN
Declare @id int
Select @id = Id from inserted
Declare @ModifiedOn datetime
Select @ModifiedOn=IsNull(ModifiedOn,CreatedOn)  from PaymentSLADetails  where id=@id

Insert into PaymentSLADetailsHistory (Interviewid,Inaccuratedecisionmaking,TimeManagement,QuestionsRelevance,ScoreRubricIgnorance,PoorFeedbackComments,
CandidateInteractionQuality,LimitedHandoncoding,WrongStatusUpdates,WeakInfrastructure,ProctoringIgnorance,UnethicalPractices,PoorCommunicationSkills,CreatedBy,
CreatedOn,PaymentDeduction,PaymentComplianceCount,Recommendation,IsTechQCDone)
(Select Interviewid,Inaccuratedecisionmaking,TimeManagement,QuestionsRelevance,ScoreRubricIgnorance,PoorFeedbackComments,
CandidateInteractionQuality,LimitedHandoncoding,WrongStatusUpdates,WeakInfrastructure,ProctoringIgnorance,UnethicalPractices,PoorCommunicationSkills,CreatedBy,
@ModifiedOn,PaymentDeduction,PaymentComplianceCount,Recommendation,IsTechQCDone From PaymentSLADetails where id=@id)
END
GO