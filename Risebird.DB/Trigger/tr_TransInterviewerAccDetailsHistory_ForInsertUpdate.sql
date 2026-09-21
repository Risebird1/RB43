
IF EXISTS (SELECT 1 FROM sys.triggers
           WHERE Name = 'tr_TransInterviewerAccDetailsHistory_ForInsertUpdate')
BEGIN
    Drop trigger tr_TransInterviewerAccDetailsHistory_ForInsertUpdate
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TRIGGER tr_TransInterviewerAccDetailsHistory_ForInsertUpdate
ON TransInterviewerAccDetails
FOR INSERT, UPDATE
AS
BEGIN
 Declare @id int
 Declare @ActionType varchar(15)

 Select @id = Id from inserted

 IF EXISTS (SELECT 1 FROM deleted)
	SET @ActionType = 'Updated'
 ELSE
	SET @ActionType = 'Inserted'

 insert into TransInterviewerAccDetailsHistory
 (Id, InterviewerProfileId, AccountName, AccountNumber, IFSCCode, UPIId, PAN,
 IsBankVerified, VerifiedOn, VerifiedAccountNumber, VerifiedIFSCCode, VerifiedAccountName, NameAtBank, CashfreeReferenceId,
 IsPanVerified, PanVerifiedOn, VerifiedPAN, NameAtPan, PanVerificationReferenceId,
 PerformedAt, PerformedBy, ActionType)

 (select Id, InterviewerProfileId, AccountName, AccountNumber, IFSCCode, UPIId, PAN,
 IsBankVerified, VerifiedOn, VerifiedAccountNumber, VerifiedIFSCCode, VerifiedAccountName, NameAtBank, CashfreeReferenceId,
 IsPanVerified, PanVerifiedOn, VerifiedPAN, NameAtPan, PanVerificationReferenceId,
 dateadd(MINUTE, 330, getdate()), SUSER_NAME(), @ActionType
 from TransInterviewerAccDetails where Id = @id)

END

