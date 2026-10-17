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
@InterviewerVideo varchar(250) ,   
@CorrectInterview varchar(15),  
@FeedBackComment varchar(max),
@AttendanceJoinCompliance varchar(250),
@AIContradictionScore varchar(250),
@SourcePage varchar(30) = NULL   -- the page's hidden Pagetype: PotentialRisk | HighRisk log a review row (QC Activity); others ignored
AS      
BEGIN      
Declare @Userid int           
Declare @PaymentDeductionPct int = @PaymentDeduction   -- @PaymentDeduction is turned into an amount below
Declare @isQcDonecheck bit   
Declare @isApprovedCheck bit   
Declare @isTechQcDonecheck bit      
Select  @isQcDonecheck=ISNULL(isQcDone,0),@isApprovedCheck=ISNULL(IsApproved,0),@isTechQcDonecheck=ISNULL(IsTechQCDone,0)  from PaymentSLADetails where Interviewid=@Interviewid    
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
if(@isApprovedCheck=1)    
begin    
set @isapproved=1    
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
PaymentComplianceCount=@PaymentComplianceCount,VideoEnable=@VideoEnable,isQcDone=@isQcDone,Recommendation=@Recommendation,IsTechQCDone= @techqc,RecommendationComments=@RecommendationComments,InterviewerVideo=@InterviewerVideo,  
CorrectInterview=@CorrectInterview,AttendanceJoinCompliance=@AttendanceJoinCompliance,AIContradictionScore=@AIContradictionScore  
where Interviewid=@Interviewid      
end      
else      
begin      
Insert into PaymentSLADetails(Interviewid,Inaccuratedecisionmaking,TimeManagement,QuestionsRelevance,ScoreRubricIgnorance,PoorFeedbackComments,CandidateInteractionQuality,      
LimitedHandoncoding,WrongStatusUpdates,WeakInfrastructure,ProctoringIgnorance,UnethicalPractices,PoorCommunicationSkills,CreatedBy,CreatedOn,PaymentDeduction,PaymentComplianceCount,VideoEnable,isQcDone,Recommendation,IsTechQCDone,RecommendationComments,InterviewerVideo,CorrectInterview,AttendanceJoinCompliance,AIContradictionScore)       
values(@Interviewid,@Inaccuratedecisionmaking,@TimeManagement,@QuestionsRelevance,@ScoreRubricIgnorance,@PoorFeedbackComments,@CandidateInteractionQuality,      
@LimitedHandoncoding,@WrongStatusUpdates,@WeakInfrastructure,@ProctoringIgnorance,@UnethicalPractices,@PoorCommunicationSkills,@Emailid,@CreatedOn,@PaymentDeduction,@PaymentComplianceCount,@VideoEnable,@isQcDone,@Recommendation,@techqc,@RecommendationComments,@InterviewerVideo,@CorrectInterview,@AttendanceJoinCompliance,@AIContradictionScore)       
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
Insert into ClientCommentsForReport(Comment, InterviewId, CreatedBy, CreatedAt, Rating) values (@FeedBackComment, @Interviewid, @Emailid, @CreatedOn, @Rating)        
end   
--if(@InterviewerVideo like '%Video Off When Mandatory%')  
--begin  
-- IF EXISTS(select * from RedoInterviews where interviewid=@interviewid)  
-- BEGIN  
-- Update RedoInterviews set  interviewid=@interviewid,ModifiedOn=@CreatedOn,ModifiedBy=@Emailid,Type='Tech QC',Rejected=0  
-- where interviewid=@interviewid and ISNULL(Approved,0)=0  
-- END  
-- ELSE  
-- BEGIN  
-- Insert into RedoInterviews(interviewid,Createdon,CreatedBy,ModifiedOn,ModifiedBy,Type,Approved) values  
-- (@interviewid,@CreatedOn,@Emailid,@CreatedOn,@Emailid,'Tech QC',0)  
-- END   
--end  
else  
begin  
 IF EXISTS(select * from RedoInterviews where interviewid=@interviewid and ISNULL(Approved,0)=0 and Type='Tech QC')  
 BEGIN  
 Update RedoInterviews set ModifiedOn=@CreatedOn,ModifiedBy=@Emailid,Type='Tech QC',Rejected=1  
 where interviewid=@interviewid and ISNULL(Approved,0)=0 and Type='Tech QC'  
 END  
end  

-- QC Activity: a review saved from Potential Risk by Candidate / the High-Risk Reports tab is logged as
-- PotentialRiskReview / HighRiskReview (Reason "Interview #<id>: ..."). usp_QCActivity_CollectRows moves the
-- matching ReportReviewed row to that page's column; the log row itself is not counted again.
SET @SourcePage = NULLIF(LTRIM(RTRIM(@SourcePage)), '')
IF @SourcePage IN ('PotentialRisk', 'HighRisk')
   AND OBJECT_ID(N'dbo.InterviewerActionLog', N'U') IS NOT NULL
   AND NULLIF(LTRIM(RTRIM(@Emailid)), '') IS NOT NULL
BEGIN
    Declare @RvIvEmail varchar(200) = (Select TOP 1 LTRIM(RTRIM(interviewer)) From InterviewMeetings where id=@Interviewid)
    INSERT INTO dbo.InterviewerActionLog (InterviewerUserId, InterviewerEmail, ActionType, Reason, ActedBy, Source)
    SELECT (Select TOP 1 ul.id From UserList ul where ul.EmailId=@RvIvEmail ORDER BY ul.id DESC),
           @RvIvEmail,
           @SourcePage + 'Review',
           LEFT(N'Interview #' + CAST(@Interviewid AS NVARCHAR(20)) + N': '
                + ISNULL(NULLIF(LTRIM(RTRIM(@RecommendationComments)), ''), N'report reviewed')
                + CASE WHEN @Rating > 0 THEN N' (rated ' + CAST(@Rating AS NVARCHAR(10)) + N'/5)' ELSE N'' END
                + N'; deduction ' + CAST(ISNULL(@PaymentDeductionPct, 0) AS NVARCHAR(10)) + N'%', 500),
           LEFT(LTRIM(RTRIM(@Emailid)), 200),
           'usp_AddorUpdatePaymentSLADetails:' + @SourcePage
END
END      
GO