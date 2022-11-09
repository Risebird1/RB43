IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_AddMCQCandidatesPremium'))
BEGIN
    DROP PROCEDURE dbo.usp_AddMCQCandidatesPremium
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

Create Procedure usp_AddMCQCandidatesPremium  
@PartitionKey varchar(50),  
@MCQId varchar(50),  
@Skills varchar(100),  
@TotalExprience int,  
@CandidateEmail varchar(200),  
@ApplyingCompany varchar(50),  
@ClientHR varchar(200),  
@CandidateScore int,  
@TotalQuestions int,  
@VideoLink varchar(1000),  
@FeedbackComment varchar(500),  
@Feedbackreating int  
AS  
BEGIN  
Insert into MCQCandidatesPremium(PartitionKey,MCQId,Skills,TotalExprience,CandidateEmail,ApplyingCompany,ClientHR,CandidateScore,TotalQuestions,VideoLink,FeedbackComment,Feedbackreating,AttemptedDate) values  
(@PartitionKey,@MCQId,@Skills,@TotalExprience,@CandidateEmail,@ApplyingCompany,@ClientHR,@CandidateScore,@TotalQuestions,@VideoLink,@FeedbackComment,@Feedbackreating,Getdate())  
END
GO