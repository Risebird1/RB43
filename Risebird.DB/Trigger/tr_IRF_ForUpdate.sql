
IF EXISTS (SELECT 1 FROM sys.triggers 
           WHERE Name = 'tr_IRF_ForUpdate')
BEGIN
    Drop trigger tr_IRF_ForUpdate
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TRIGGER [dbo].[tr_IRF_ForUpdate]
ON [dbo].[IRF]
FOR UPDATE
AS
BEGIN

 Declare @id int
 Select @id = Id from inserted
 
 insert into IRFHistory 
  (IRFId,	ClientName,	JDTiltle,	CompanyType,	ClientId,	JobCountry, StartDate,
 EndDate,	JDLink,	NoOfPosition,	NoOfIntterview,	RisebirdHr,	PerMintuteRate,
 IsDeleted,	JobLevel,	TotalInvites,	CalendlyLink,	CreatedOn,	TotalBudget,
 Discount,	IsBiometricEnabled,	PriorityLevel	,IsPaused,	CompanyDetails,	Comment,
 idProgress,	BussinessType,	Heuristicsscore,	PrimarySkillInvite	,BillingOwner,
 AssessmentGroupId,	IsQCRequired,	IsJoinabilitySelectabilityRequired,	IsGeneralSkillRequired,
 AudioComment,	CreatedFromTemplateId,	ModifiedOn,	MeetingPlatform,
 SendIntervieweravailbilityOn,	IsAadhaarVerificationRequired,	IsCandidateavailability,	IsMeetingNotificationSend, IsMeetingAutoCancelled,id,
 ActionType, PerformedAt,InterviewDuration)

 (select IRFId,	ClientName,	JDTiltle,	CompanyType,	ClientId,	JobCountry, StartDate,
 EndDate,	JDLink,	NoOfPosition,	NoOfIntterview,	RisebirdHr,	PerMintuteRate,
 IsDeleted,	JobLevel,	TotalInvites,	CalendlyLink,	CreatedOn,	TotalBudget,
 Discount,	IsBiometricEnabled,	PriorityLevel	,IsPaused,	CompanyDetails,	Comment,
 idProgress,	BussinessType,	Heuristicsscore,	PrimarySkillInvite	,BillingOwner,
 AssessmentGroupId,	IsQCRequired,	IsJoinabilitySelectabilityRequired,	IsGeneralSkillRequired,
 AudioComment,	CreatedFromTemplateId,	ModifiedOn,	MeetingPlatform,
 SendIntervieweravailbilityOn,	IsAadhaarVerificationRequired,	IsCandidateavailability,	IsMeetingNotificationSend, IsMeetingAutoCancelled,id,
 'Updated', DATEADD(MINUTE, 330, getdate()),InterviewDuration
 from IRF where id = @id) 
 

END
GO
