IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_InsertInterviewerSuspicious'))
BEGIN
    DROP PROCEDURE dbo.usp_InsertInterviewerSuspicious
END


SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

Create Procedure [dbo].[usp_InsertInterviewerSuspicious]
@Comment varchar(250),
@Email varchar(50),
@CreatedDate datetime,
@CreatedBy varchar(50),
@SuspiciousStatus bit
AS
Begin
 DECLARE @UserId int=(select id from UserList where EmailId=@Email)
 Insert into SuspiciousInterviewer(UserId,Comment,Createddate,CreatedBy,SuspiciousStatus) values(@UserId,@Comment,@CreatedDate,@CreatedBy,@SuspiciousStatus)
 END

Go