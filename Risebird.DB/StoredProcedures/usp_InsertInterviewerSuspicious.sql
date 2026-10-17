

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_InsertInterviewerSuspicious'))
BEGIN
    DROP PROCEDURE dbo.usp_InsertInterviewerSuspicious
END


SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

-- Deploy after Tables/SuspiciousInterviewer.sql (Comment 500, CreatedBy 200, SourcePage).
-- An unknown interviewer email raises an error, so the page shows a failure instead of a false "saved".
-- @SourcePage: page the mark came from (TotalInterviews, TRM, PotentialRiskCandidate, TrialFlagReview,
-- PaymentSla, HighRisk, 911:<action>); NULL from older callers.
Create Procedure [dbo].[usp_InsertInterviewerSuspicious]
@Comment varchar(500),
@Email varchar(200),
@CreatedDate datetime,
@CreatedBy varchar(200),
@SourcePage varchar(60) = NULL
AS
Begin
 SET NOCOUNT ON;
 DECLARE @UserId int=(select TOP 1 id from UserList where EmailId=LTRIM(RTRIM(@Email)) ORDER BY id DESC)
 IF @UserId IS NULL
 BEGIN
     RAISERROR('usp_InsertInterviewerSuspicious: no user with email %s.', 16, 1, @Email);
     RETURN;
 END
-- If EXISTS(select * from SuspiciousInterviewer where UserId=@UserId)
--begin
--update SuspiciousInterviewer set Comment=@Comment,ModifyBy=@CreatedBy,ModifyDate=@CreatedDate,SuspiciousStatus=1 where UserId=@UserId
--end
--else
--begin
 IF COL_LENGTH(N'dbo.SuspiciousInterviewer', N'SourcePage') IS NOT NULL
     EXEC sp_executesql N'
     Insert into SuspiciousInterviewer(UserId,Comment,Createddate,CreatedBy,SuspiciousStatus,SourcePage)
     values(@UserId,@Comment,@CreatedDate,@CreatedBy,1,NULLIF(LTRIM(RTRIM(@SourcePage)), ''''))',
     N'@UserId int, @Comment varchar(500), @CreatedDate datetime, @CreatedBy varchar(200), @SourcePage varchar(60)',
     @UserId = @UserId, @Comment = @Comment, @CreatedDate = @CreatedDate, @CreatedBy = @CreatedBy, @SourcePage = @SourcePage;
 ELSE
     Insert into SuspiciousInterviewer(UserId,Comment,Createddate,CreatedBy,SuspiciousStatus) values(@UserId,@Comment,@CreatedDate,@CreatedBy,1)
 --end
 END
Go
