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
@isapproved int,    
@PaymentComplianceCount int,    
@Rating int,  
@VideoEnable bit,  
@isQcDone bit,  
@techqc bit,  
@Recommendation varchar(15),  
@RecommendationComments varchar(500),  
@InterviewerVideo varchar(250)  
AS    
BEGIN    
Declare @Userid int         
Declare @isQcDonecheck bit    
Declare @isTechQcDonecheck bit    
Select  @isQcDonecheck=ISNULL(isQcDone,0) from PaymentSLADetails where Interviewid=@Interviewid  
Select  @isTechQcDonecheck=ISNULL(IsTechQCDone,0) from PaymentSLADetails where Interviewid=@Interviewid  
if(@techqc=1)  
begin  
Update TechQualityDetails set IsCompleted=1 where interviewid=@Interviewid  
end  
if(@isQcDonecheck=1)  
begin  
set @isQcDone=1  
end  
if(@isTechQcDonecheck=1)  
begin  
set @techqc=1  
end  
Select @Userid=id From UserList where Emailid=@Emailid      
if(@Rating<5)  
begin  
IF EXISTS(Select 1 from PaymentSLADetails where Interviewid=@Interviewid)          
begin     
Update PaymentSLADetails set Inaccuratedecisionmaking=@Inaccuratedecisionmaking,TimeManagement=@TimeManagement,QuestionsRelevance=@QuestionsRelevance,    
ScoreRubricIgnorance=@ScoreRubricIgnorance,PoorFeedbackComments=@PoorFeedbackComments,CandidateInteractionQuality=@CandidateInteractionQuality,    
LimitedHandoncoding=@LimitedHandoncoding,WrongStatusUpdates=@WrongStatusUpdates,WeakInfrastructure=@WeakInfrastructure,ProctoringIgnorance=@ProctoringIgnorance,    
UnethicalPractices=@UnethicalPractices,PoorCommunicationSkills=@PoorCommunicationSkills,ModifiedBy=@Emailid,ModifiedOn=@CreatedOn,PaymentDeduction=@PaymentDeduction,isapproved=@isapproved,    
PaymentComplianceCount=@PaymentComplianceCount,VideoEnable=@VideoEnable,isQcDone=@isQcDone,Recommendation=@Recommendation,IsTechQCDone= @techqc,RecommendationComments=@RecommendationComments,InterviewerVideo=@InterviewerVideo   
where Interviewid=@Interviewid    
end    
else    
begin    
Insert into PaymentSLADetails(Interviewid,Inaccuratedecisionmaking,TimeManagement,QuestionsRelevance,ScoreRubricIgnorance,PoorFeedbackComments,CandidateInteractionQuality,    
LimitedHandoncoding,WrongStatusUpdates,WeakInfrastructure,ProctoringIgnorance,UnethicalPractices,PoorCommunicationSkills,CreatedBy,CreatedOn,PaymentDeduction,PaymentComplianceCount,VideoEnable,isQcDone,Recommendation,IsTechQCDone,RecommendationComments,InterviewerVideo)     
values(@Interviewid,@Inaccuratedecisionmaking,@TimeManagement,@QuestionsRelevance,@ScoreRubricIgnorance,@PoorFeedbackComments,@CandidateInteractionQuality,    
@LimitedHandoncoding,@WrongStatusUpdates,@WeakInfrastructure,@ProctoringIgnorance,@UnethicalPractices,@PoorCommunicationSkills,@Emailid,@CreatedOn,@PaymentDeduction,@PaymentComplianceCount,@VideoEnable,@isQcDone,@Recommendation,@techqc,@RecommendationComments,@InterviewerVideo)     
end    
end  
If(@isapproved=1)    
begin    
Declare @InterviewerUserid int    
Declare @ClientId int    
Declare @InterviewRate int    
Select @ClientId=IRF.ClientId From InterviewMeetings im join IRF on IRF.IRFId=im.IRFId where im.id=@Interviewid    
Select @InterviewerUserid=ul.id from UserList ul join InterviewMeetings im on im.interviewer=ul.EmailId where im.id=@Interviewid    
Select @InterviewRate= Case when IRF.IsMarketPlaceChannel=1 then IRF.RateComputedForClient else Cl.PerInterviewRate end  From IRF join InterviewMeetings im on im.IRFId=IRF.IRFId Join Client cl on cl.id=IRF.ClientId where im.id=@Interviewid    
Update InterviewPoints set PaymentSLA=@PaymentDeduction where Interviewid=@Interviewid and Userid=@InterviewerUserid and Type='Interview'       
set @PaymentDeduction=@PaymentDeduction*@InterviewRate/100    
Insert into MoneyProtectionWallet(Clientid,Points,AddedOn,Interviewid,IsConverted,Rate) values(@ClientId,@PaymentDeduction,@CreatedOn,@Interviewid,0,@InterviewRate)    
end    
if(@Rating>0)    
begin    
Update InterviewComments set ReportAccepted=1,ReportAcceptedBy=@Emailid where Interviewid=@Interviewid    
Insert into ClientCommentsForReport(Comment, InterviewId, CreatedBy, CreatedAt, Rating) values ('', @Interviewid, @Emailid, @CreatedOn, @Rating)      
end 
if(@InterviewerVideo like '%Video Off When Mandatory%')
begin
	IF EXISTS(select * from RedoInterviews where interviewid=@interviewid)
	BEGIN
	Update RedoInterviews set  interviewid=@interviewid,ModifiedOn=@CreatedOn,ModifiedBy=@Emailid,Type='Tech QC',Rejected=0
	where interviewid=@interviewid and ISNULL(Approved,0)=0
	END
	ELSE
	BEGIN
	Insert into RedoInterviews(interviewid,Createdon,CreatedBy,ModifiedOn,ModifiedBy,Type,Approved) values
	(@interviewid,@CreatedOn,@Emailid,@CreatedOn,@Emailid,'Tech QC',0)
	END 
end
else
begin
	IF EXISTS(select * from RedoInterviews where interviewid=@interviewid and ISNULL(Approved,0)=0 and Type='Tech QC')
	BEGIN
	Update RedoInterviews set ModifiedOn=@CreatedOn,ModifiedBy=@Emailid,Type='Tech QC',Rejected=1
	where interviewid=@interviewid and ISNULL(Approved,0)=0 and Type='Tech QC'
	END
end
END    


GO