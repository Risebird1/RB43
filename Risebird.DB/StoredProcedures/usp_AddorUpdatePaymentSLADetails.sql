IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_AddorUpdatePaymentSLADetails'))
BEGIN
    DROP PROCEDURE dbo.usp_AddorUpdatePaymentSLADetails
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create Procedure usp_AddorUpdatePaymentSLADetails
@Interviewid int,
@Emailid varchar(150),
@PaymentDeduction int,
@Inaccuratedecisionmaking varchar(50),
@TimeManagement varchar(50),
@QuestionsRelevance varchar(50),
@ScoreRubricIgnorance varchar(50),
@PoorFeedbackComments varchar(50),
@CandidateInteractionQuality varchar(50),
@LimitedHandoncoding varchar(50),
@WrongStatusUpdates varchar(50),
@WeakInfrastructure varchar(50),
@ProctoringIgnorance varchar(50),
@UnethicalPractices varchar(50),
@PoorCommunicationSkills varchar(50),
@isapproved bit
AS
BEGIN
Declare @Userid int       
Select @Userid=id From UserList where Emailid=@Emailid      
IF EXISTS(Select 1 from PaymentSLADetails where Interviewid=@Interviewid)      
begin 
Update PaymentSLADetails set Inaccuratedecisionmaking=@Inaccuratedecisionmaking,TimeManagement=@TimeManagement,QuestionsRelevance=@QuestionsRelevance,
ScoreRubricIgnorance=@ScoreRubricIgnorance,PoorFeedbackComments=@PoorFeedbackComments,CandidateInteractionQuality=@CandidateInteractionQuality,
LimitedHandoncoding=@LimitedHandoncoding,WrongStatusUpdates=@WrongStatusUpdates,WeakInfrastructure=@WeakInfrastructure,ProctoringIgnorance=@ProctoringIgnorance,
UnethicalPractices=@UnethicalPractices,PoorCommunicationSkills=@PoorCommunicationSkills,CreatedBy=@Emailid,CreatedOn=Getdate(),PaymentDeduction=@PaymentDeduction,isapproved=@isapproved
where Interviewid=@Interviewid
end
else
begin
Insert into PaymentSLADetails(Interviewid,Inaccuratedecisionmaking,TimeManagement,QuestionsRelevance,ScoreRubricIgnorance,PoorFeedbackComments,CandidateInteractionQuality,
LimitedHandoncoding,WrongStatusUpdates,WeakInfrastructure,ProctoringIgnorance,UnethicalPractices,PoorCommunicationSkills,CreatedBy,CreatedOn,PaymentDeduction) 
values(@Interviewid,@Inaccuratedecisionmaking,@TimeManagement,@QuestionsRelevance,@ScoreRubricIgnorance,@PoorFeedbackComments,@CandidateInteractionQuality,
@LimitedHandoncoding,@WrongStatusUpdates,@WeakInfrastructure,@ProctoringIgnorance,@UnethicalPractices,@PoorCommunicationSkills,@Emailid,Getdate(),@PaymentDeduction) 
end
END
GO