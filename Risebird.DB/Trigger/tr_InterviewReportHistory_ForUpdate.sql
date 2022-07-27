
IF EXISTS (SELECT 1 FROM sys.triggers 
           WHERE Name = 'tr_InterviewReportHistory_ForUpdate')
BEGIN
    Drop trigger tr_InterviewReportHistory_ForUpdate
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TRIGGER tr_InterviewReportHistory_ForUpdate
ON InterviewReport
FOR UPDATE
AS
BEGIN
 Declare @id int
 Select @id = Id from inserted

 insert into InterviewReportHistory
 (id,	IRFId,	InterviewId, CandidateName,	confirmationStatus,	OverallScore, Rating, TechSkillComments,
 SkillGeneralComments,	JoinabilityComments, SelectabilityComments,	ProctoringCount, CreatedOn,	ModifiedOn,
 CreatedBy,	ModifiedBy,	PerformedAt, PerformedBy, ActionType)

 (select id,	IRFId,	InterviewId, CandidateName,	confirmationStatus,	OverallScore, Rating, TechSkillComments,
 SkillGeneralComments,	JoinabilityComments, SelectabilityComments,	ProctoringCount, CreatedOn,	ModifiedOn,
 CreatedBy,	ModifiedBy,	DATEADD(minute, 330, getdate()), ModifiedBy, 'Updated'
 from InterviewReport where id = @id )
 
 

END
