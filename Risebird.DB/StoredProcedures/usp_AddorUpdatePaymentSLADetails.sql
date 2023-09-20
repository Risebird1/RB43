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
@CreatedOn datetime,
@PaymentDeduction int,
@Inaccuratedecisionmaking varchar(250),
@TimeManagement varchar(250),
@QuestionsRelevance varchar(250),
@ScoreRubricIgnorance varchar(250),
@PoorFeedbackComments varchar(250),
@CandidateInteractionQuality varchar(250),
@LimitedHandoncoding varchar(250),
@WrongStatusUpdates varchar(250),
@WeakInfrastructure varchar(250),
@ProctoringIgnorance varchar(250),
@UnethicalPractices varchar(250),
@PoorCommunicationSkills varchar(250),
@isapproved int
AS
BEGIN
Declare @Userid int       
Select @Userid=id From UserList where Emailid=@Emailid      
IF EXISTS(Select 1 from PaymentSLADetails where Interviewid=@Interviewid)      
begin 
Update PaymentSLADetails set Inaccuratedecisionmaking=@Inaccuratedecisionmaking,TimeManagement=@TimeManagement,QuestionsRelevance=@QuestionsRelevance,
ScoreRubricIgnorance=@ScoreRubricIgnorance,PoorFeedbackComments=@PoorFeedbackComments,CandidateInteractionQuality=@CandidateInteractionQuality,
LimitedHandoncoding=@LimitedHandoncoding,WrongStatusUpdates=@WrongStatusUpdates,WeakInfrastructure=@WeakInfrastructure,ProctoringIgnorance=@ProctoringIgnorance,
UnethicalPractices=@UnethicalPractices,PoorCommunicationSkills=@PoorCommunicationSkills,ModifiedBy=@Emailid,ModifiedOn=@CreatedOn,PaymentDeduction=@PaymentDeduction,isapproved=@isapproved
where Interviewid=@Interviewid
end
else
begin
Insert into PaymentSLADetails(Interviewid,Inaccuratedecisionmaking,TimeManagement,QuestionsRelevance,ScoreRubricIgnorance,PoorFeedbackComments,CandidateInteractionQuality,
LimitedHandoncoding,WrongStatusUpdates,WeakInfrastructure,ProctoringIgnorance,UnethicalPractices,PoorCommunicationSkills,CreatedBy,CreatedOn,PaymentDeduction) 
values(@Interviewid,@Inaccuratedecisionmaking,@TimeManagement,@QuestionsRelevance,@ScoreRubricIgnorance,@PoorFeedbackComments,@CandidateInteractionQuality,
@LimitedHandoncoding,@WrongStatusUpdates,@WeakInfrastructure,@ProctoringIgnorance,@UnethicalPractices,@PoorCommunicationSkills,@Emailid,@CreatedOn,@PaymentDeduction) 
end
If(@isapproved=1)
begin
Declare @InterviewerUserid int
Select @InterviewerUserid=ul.id from UserList ul join InterviewMeetings im on im.interviewer=ul.EmailId where im.id=@Interviewid
Update InterviewPoints set PaymentSLA=@PaymentDeduction where Interviewid=@Interviewid and Userid=@InterviewerUserid and Type='Interview'    
end
END
GO