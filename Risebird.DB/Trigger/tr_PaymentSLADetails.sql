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

Insert into PaymentSLADetailsHistory (Interviewid,Inaccuratedecisionmaking,TimeManagement,QuestionsRelevance,ScoreRubricIgnorance,PoorFeedbackComments,
CandidateInteractionQuality,LimitedHandoncoding,WrongStatusUpdates,WeakInfrastructure,ProctoringIgnorance,UnethicalPractices,PoorCommunicationSkills,CreatedBy,
CreatedOn,PaymentDeduction,PaymentComplianceCount)
(Select Interviewid,Inaccuratedecisionmaking,TimeManagement,QuestionsRelevance,ScoreRubricIgnorance,PoorFeedbackComments,
CandidateInteractionQuality,LimitedHandoncoding,WrongStatusUpdates,WeakInfrastructure,ProctoringIgnorance,UnethicalPractices,PoorCommunicationSkills,CreatedBy,
CreatedOn,PaymentDeduction,PaymentComplianceCount From PaymentSLADetails where id=@id)
END
GO