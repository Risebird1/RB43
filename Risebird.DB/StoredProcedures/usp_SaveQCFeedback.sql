
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_SaveQCFeedback'))
BEGIN
    DROP PROCEDURE dbo.usp_SaveQCFeedback
END
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

 
create PROCEDURE [dbo].[usp_SaveQCFeedback]     
@InterviewId int,
@QCStatus int,
@QCComment varchar(max),
@FraudLipSync bit,
@QuestionsRelevance bit,
@HandsOnCoding bit,
@BiasAndCandidateHandling bit,
@TechnicalCommunicationSkills bit,
@ChapteringCompleted bit,
@QuestionsFilled bit,
@TechSkillsMetric bit,
@SelectabilityMetric bit,
@JoinabilityMetric bit,
@GeneralSkillMetric bit,
@CommentsFilled bit,
@MeaningfulAssessments bit,
@CorrectEnglishGrammar bit,
@ProctoringReportFilled bit


AS      
BEGIN      
   update InterviewComments set QCStatus=@QCStatus, QCComment=@QCComment where InterviewId=@InterviewId
   
   if not exists(select 1 from QCFeedbackReport where InterviewId = @InterviewId)
   begin

   insert into QCFeedbackReport
			([InterviewId]
            ,[FraudLipSync]
            ,[QuestionsRelevance]
            ,[HandsOnCoding]
		    ,[BiasAndCandidateHandling]
            ,[TechnicalCommunicationSkills]
            ,[ChapteringCompleted]
            ,[QuestionsFilled]
            ,[TechSkillsMetric]
            ,[SelectabilityMetric]
            ,[JoinabilityMetric]
            ,[GeneralSkillMetric]
            ,[CommentsFilled]
		    ,[MeaningfulAssessments]
		    ,[CorrectEnglishGrammar]
		    ,[ProctoringReportFilled]		   
		   )
		   values
		   (@InterviewId,		   
@FraudLipSync,
@QuestionsRelevance,
@HandsOnCoding,
@BiasAndCandidateHandling,
@TechnicalCommunicationSkills,
@ChapteringCompleted,
@QuestionsFilled,
@TechSkillsMetric,
@SelectabilityMetric,
@JoinabilityMetric,
@GeneralSkillMetric,
@CommentsFilled,
@MeaningfulAssessments,
@CorrectEnglishGrammar,
@ProctoringReportFilled)
   

   end
   else
   begin
   
   update QCFeedbackReport set 
   [FraudLipSync]=@FraudLipSync,
   [QuestionsRelevance]=@QuestionsRelevance,
   [HandsOnCoding]=@HandsOnCoding,
   [BiasAndCandidateHandling] =@BiasAndCandidateHandling,
   [TechnicalCommunicationSkills]=@TechnicalCommunicationSkills,
   [ChapteringCompleted]=@ChapteringCompleted,
   [QuestionsFilled]=@QuestionsFilled,
   [TechSkillsMetric]=@TechSkillsMetric,
   [SelectabilityMetric]=@SelectabilityMetric,
   [JoinabilityMetric]=@JoinabilityMetric,
   [GeneralSkillMetric]=@GeneralSkillMetric,
   [CommentsFilled]=@CommentsFilled,
   [MeaningfulAssessments]=@MeaningfulAssessments,
   [CorrectEnglishGrammar]=@CorrectEnglishGrammar,
   [ProctoringReportFilled]=@ProctoringReportFilled
   where InterviewId=@InterviewId
   end
END   
GO
