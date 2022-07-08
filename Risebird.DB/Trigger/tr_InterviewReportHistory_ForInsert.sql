
IF EXISTS (SELECT 1 FROM sys.triggers 
           WHERE Name = 'tr_InterviewReportHistory_ForInsert')
BEGIN
    Drop trigger tr_InterviewReportHistory_ForInsert
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TRIGGER tr_InterviewReportHistory_ForInsert
ON InterviewReport
FOR INSERT
AS
BEGIN
 Declare @id int
 Select @id = Id from inserted

 insert into InterviewReportHistory
 (id,	IRFId,	InterviewId, CandidateName,	confirmationStatus,	OverallScore, Rating, TechSkillComments,
 SkillGeneralComments,	JoinabilityComments, SelectabilityComments,	ProctoringCount, CreatedOn,	ModifiedOn,
 CreatedBy,	ModifiedBy,	PerformedAt, PerformedBy)

 (select id,	IRFId,	InterviewId, CandidateName,	confirmationStatus,	OverallScore, Rating, TechSkillComments,
 SkillGeneralComments,	JoinabilityComments, SelectabilityComments,	ProctoringCount, CreatedOn,	ModifiedOn,
 CreatedBy,	ModifiedBy,	CreatedOn, CreatedBy
 from InterviewReport where id = @id )
 
 

END
