
IF EXISTS (SELECT 1 FROM sys.triggers 
           WHERE Name = 'tr_ClonableSkillChannel_ForUpdate')
BEGIN
    Drop trigger tr_ClonableSkillChannel_ForUpdate
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TRIGGER tr_ClonableSkillChannel_ForUpdate
ON ClonableSkillChannel
FOR UPDATE
AS
BEGIN
 Declare @id int
 Select @id = Id from inserted
 
 insert into ClonableSkillChannelHistory 
 (id,	CreatedFromIRFId,	Name,	CreatedOn,	ModifiedOn,	IsJoinSelectGenskillsRequired,	CreatedBy, ModifiedBy,
 MeetingPlatform,	IsAadhaarVerificationRequired, ActionType, PerformedAt, PerformedBy)

 (select id,	CreatedFromIRFId,	Name,	CreatedOn,	ModifiedOn,	IsJoinSelectGenskillsRequired,	CreatedBy, ModifiedBy,
 MeetingPlatform,	IsAadhaarVerificationRequired, 'Updated', DATEADD(MINUTE, 330, getdate()), ModifiedBy
 from ClonableSkillChannel where id = @id)
END


