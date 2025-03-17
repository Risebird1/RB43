IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_UpdateCandidateChecks'))
BEGIN
    DROP PROCEDURE dbo.usp_UpdateCandidateChecks
END
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE usp_UpdateCandidateChecks
@irfid int,
@CandidateCheckvalue bit,
@CandidateCheckName varchar(50)
AS
BEGIN
if(@CandidateCheckName like '%CandidateIdverification%')
begin
Update IRF set CandidateIdverification=@CandidateCheckvalue where id=@irfid
end
else if(@CandidateCheckName like '%CodeSnippetRequired%')
begin
Update IRF set CodeSnippetRequired=@CandidateCheckvalue where id=@irfid
end
END
Go