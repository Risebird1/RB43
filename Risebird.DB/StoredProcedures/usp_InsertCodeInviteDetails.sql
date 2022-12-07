
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID(' dbo.usp_InsertCodeInviteDetails'))
BEGIN
    DROP PROCEDURE dbo.usp_InsertCodeInviteDetails
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE Procedure usp_InsertCodeInviteDetails
@UserName varchar(200),
@Experience [decimal](10, 2),
@ProgrammingLanguage VARCHAR(MAX) ,
@DifficultyLevel varchar(20),
@InviteStartTime varchar(20),
@InviteEndTime varchar(20),
@TimeZone varchar(10),
@invite_id varchar(200),
@invite_UrL varchar(200),
@CreatedAt varchar (20),
@CreatedBy varchar (20),
@ModifiedBy varchar (20) NULL,
@candidateEmail varchar(200),
@botURL varchar(max)
AS  
BEGIN  
insert into CodeInviteDetails (
UserName,
Experience,
ProgrammingLanguage,
DifficultyLevel,
InviteStartTime,
InviteEndTime ,
TimeZone,
inviteId, invite_UrL,CreatedAt,CreatedBy,ModifiedAt,ModifiedBy,candidateEmail,botURL )values(       
@UserName,
@Experience,
@ProgrammingLanguage,
@DifficultyLevel,
@InviteStartTime,
@InviteEndTime,
@TimeZone,
@invite_id,
@invite_UrL,@CreatedAt, @CreatedBy,@CreatedAt, @ModifiedBy,@candidateEmail,@botURL )

END


GO


